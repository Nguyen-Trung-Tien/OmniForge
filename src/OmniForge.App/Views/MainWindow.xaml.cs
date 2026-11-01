using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OmniForge.App.ViewModels;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Services;
using OmniForge.Services.Navigation;

namespace OmniForge.App.Views;

/// <summary>
/// Root Shell Window providing fluent layout, custom titlebar, MicaAlt backdrop, live theme switching, and navigation host.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        ISettingsService settingsService,
        ILocalizationService localizationService)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Configure Win32 AppWindow Icon from high-DPI multi-layer app.ico
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        if (navigationService is NavigationService navService)
        {
            navService.Initialize(ContentFrame);
            navService.Navigated += (sender, pageType) =>
            {
                if (ContentFrame.Content is FrameworkElement page && Content is FrameworkElement root)
                {
                    page.RequestedTheme = root.RequestedTheme;
                }
            };
        }

        // Initialize persistent theme, window geometry, and navigation state
        InitializeWindowSettings();

        // Listen for language changes to update native Settings item text
        _localizationService.LanguageChanged += (_, _) => DispatcherQueue?.TryEnqueue(UpdateLocalizedChrome);
        NavView.Loaded += (_, _) => UpdateLocalizedChrome();

        // Trigger initial item binding and default navigation
        ViewModel.RefreshNavigationItems();
    }

    private void UpdateLocalizedChrome()
    {
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = _localizationService.GetString(LocalizationKeys.Nav_Settings);
        }
    }

    private void InitializeWindowSettings()
    {
        // 1. Initial theme application
        _ = _settingsService.GetThemeAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                DispatcherQueue?.TryEnqueue(() => ApplyTheme(t.Result));
            }
            else
            {
                _ = t.Exception;
            }
        });

        // 2. Initial MicaAlt backdrop application
        _ = _settingsService.GetSettingAsync("General.EnableMicaAlt", true).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    SystemBackdrop = t.Result ? new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt } : null;
                });
            }
            else
            {
                _ = t.Exception;
            }
        });

        // 3. React to dynamic live setting switches (Theme, MicaAlt)
        _settingsService.SettingChanged += (sender, key) =>
        {
            if (string.Equals(key, SettingsService.ThemeSettingKey, StringComparison.OrdinalIgnoreCase))
            {
                _ = _settingsService.GetThemeAsync().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        DispatcherQueue?.TryEnqueue(() => ApplyTheme(t.Result));
                    }
                    else
                    {
                        _ = t.Exception;
                    }
                });
            }
            else if (string.Equals(key, "General.EnableMicaAlt", StringComparison.OrdinalIgnoreCase))
            {
                _ = _settingsService.GetSettingAsync("General.EnableMicaAlt", true).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        DispatcherQueue?.TryEnqueue(() =>
                        {
                            SystemBackdrop = t.Result ? new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt } : null;
                        });
                    }
                    else
                    {
                        _ = t.Exception;
                    }
                });
            }
        };

        // 4. Configure minimum window dimensions and restore persisted window dimensions
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 680;
            presenter.PreferredMinimumHeight = 500;
        }

        _ = _settingsService.GetWindowSizeAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                var (width, height) = t.Result;
                if (width >= 680 && height >= 500)
                {
                    DispatcherQueue?.TryEnqueue(() =>
                    {
                        AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                    });
                }
            }
            else
            {
                _ = t.Exception;
            }
        });

        // 5. Persist window dimensions on window close
        Closed += (sender, args) =>
        {
            var size = AppWindow.Size;
            if (size.Width >= 680 && size.Height >= 500)
            {
                _ = _settingsService.SetWindowSizeAsync(size.Width, size.Height).ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            }
        };
    }

    private void ApplyTheme(string theme)
    {
        if (Content is FrameworkElement rootElement)
        {
            var elementTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            rootElement.RequestedTheme = elementTheme;

            if (AppTitleBar is not null)
            {
                AppTitleBar.RequestedTheme = elementTheme;
            }

            if (ContentFrame.Content is FrameworkElement pageContent)
            {
                pageContent.RequestedTheme = elementTheme;
            }

            UpdateTitleBarButtonColors(elementTheme);
        }
    }

    private void UpdateTitleBarButtonColors(ElementTheme theme)
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow?.TitleBar is { } titleBar)
        {
            bool isDark = theme == ElementTheme.Dark ||
                (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);

            var foregroundColor = isDark
                ? Windows.UI.Color.FromArgb(255, 255, 255, 255)
                : Windows.UI.Color.FromArgb(255, 30, 30, 30);

            var hoverBgColor = isDark
                ? Windows.UI.Color.FromArgb(30, 255, 255, 255)
                : Windows.UI.Color.FromArgb(30, 0, 0, 0);

            titleBar.ButtonForegroundColor = foregroundColor;
            titleBar.ButtonHoverForegroundColor = foregroundColor;
            titleBar.ButtonPressedForegroundColor = foregroundColor;
            titleBar.ButtonHoverBackgroundColor = hoverBgColor;
            titleBar.ButtonInactiveForegroundColor = isDark
                ? Windows.UI.Color.FromArgb(120, 255, 255, 255)
                : Windows.UI.Color.FromArgb(120, 0, 0, 0);
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        ViewModel.GoBackCommand.Execute(null);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ViewModel.NavigateSettingsCommand.Execute(null);
            return;
        }

        NavigationItemViewModel? targetItem = null;

        if (args.SelectedItem is NavigationItemViewModel navItem)
        {
            targetItem = navItem;
        }
        else if (args.SelectedItemContainer?.Tag is NavigationItemViewModel tagItem)
        {
            targetItem = tagItem;
        }
        else if (args.SelectedItemContainer?.DataContext is NavigationItemViewModel dcItem)
        {
            targetItem = dcItem;
        }
        else if (args.SelectedItemContainer?.Content is NavigationItemViewModel contentItem)
        {
            targetItem = contentItem;
        }

        if (targetItem is not null)
        {
            ViewModel.NavigateItemCommand.Execute(targetItem);
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.ChosenSuggestion?.ToString() ?? args.QueryText;
        ViewModel.AutoSuggestQuerySubmittedCommand.Execute(query);
    }

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string selectedText)
        {
            ViewModel.AutoSuggestQuerySubmittedCommand.Execute(selectedText);
        }
    }
}
