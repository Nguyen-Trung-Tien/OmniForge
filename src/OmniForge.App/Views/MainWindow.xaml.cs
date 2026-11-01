using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniForge.App.ViewModels;
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

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, ISettingsService settingsService)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

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
        }

        // Initialize persistent theme, window geometry, and navigation state
        InitializeWindowSettings();

        // Trigger initial item binding and default navigation
        ViewModel.RefreshNavigationItems();
    }

    private void InitializeWindowSettings()
    {
        // 1. Initial theme application
        _ = _settingsService.GetThemeAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                DispatcherQueue.TryEnqueue(() => ApplyTheme(t.Result));
            }
        });

        // 2. React to dynamic live theme switches
        _settingsService.SettingChanged += (sender, key) =>
        {
            if (string.Equals(key, SettingsService.ThemeSettingKey, StringComparison.OrdinalIgnoreCase))
            {
                _ = _settingsService.GetThemeAsync().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        DispatcherQueue.TryEnqueue(() => ApplyTheme(t.Result));
                    }
                });
            }
        };

        // 3. Restore persisted window dimensions
        _ = _settingsService.GetWindowSizeAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                var (width, height) = t.Result;
                if (width >= 600 && height >= 400)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
                    });
                }
            }
        });

        // 4. Persist window dimensions on window close
        Closed += (sender, args) =>
        {
            var size = AppWindow.Size;
            if (size.Width >= 600 && size.Height >= 400)
            {
                _ = _settingsService.SetWindowSizeAsync(size.Width, size.Height);
            }
        };
    }

    private void ApplyTheme(string theme)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        ViewModel.GoBackCommand.Execute(null);
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ViewModel.NavigateSettingsCommand.Execute(null);
        }
        else if (args.InvokedItemContainer?.DataContext is NavigationItemViewModel navItem)
        {
            ViewModel.NavigateItemCommand.Execute(navItem);
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.AutoSuggestQuerySubmittedCommand.Execute(args.QueryText);
    }
}
