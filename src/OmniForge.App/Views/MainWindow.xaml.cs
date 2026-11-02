using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OmniForge.App.ViewModels;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Services;
using OmniForge.Services.Navigation;
using WinRT.Interop;

namespace OmniForge.App.Views;

/// <summary>
/// Root Shell Window providing fluent layout, custom titlebar, MicaAlt backdrop, live theme switching, and navigation host.
/// </summary>
public sealed partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private bool _isMicaEnabled = true;

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

        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged += OnRootElementActualThemeChanged;
        }

        // Initialize persistent theme, window geometry, and navigation state
        InitializeWindowSettings();

        // Listen for language changes to update native Settings item text
        _localizationService.LanguageChanged += (_, _) => DispatcherQueue?.TryEnqueue(UpdateLocalizedChrome);
        NavView.Loaded += (_, _) => UpdateLocalizedChrome();

        // Trigger initial item binding and default navigation
        ViewModel.RefreshNavigationItems();
    }

    private void OnRootElementActualThemeChanged(FrameworkElement sender, object args)
    {
        _ = _settingsService.GetThemeAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    bool isDark = DetermineIsDark(t.Result, sender.ActualTheme);
                    UpdateDwmImmersiveDarkMode(isDark);
                    UpdateTitleBarButtonColors(isDark);
                    UpdateBackdropAndBackground(isDark);
                });
            }
        });
    }

    private static bool DetermineIsDark(string themeSetting, ElementTheme actualTheme)
    {
        return themeSetting switch
        {
            "Dark" => true,
            "Light" => false,
            _ => actualTheme == ElementTheme.Dark || (actualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark)
        };
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
                DispatcherQueue?.TryEnqueue(async () =>
                {
                    _isMicaEnabled = t.Result;
                    string theme = await _settingsService.GetThemeAsync();
                    bool isDark = DetermineIsDark(theme, (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default);
                    UpdateBackdropAndBackground(isDark);
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
                        DispatcherQueue?.TryEnqueue(async () =>
                        {
                            _isMicaEnabled = t.Result;
                            string theme = await _settingsService.GetThemeAsync();
                            bool isDark = DetermineIsDark(theme, (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default);
                            UpdateBackdropAndBackground(isDark);
                        });
                    }
                    else
                    {
                        _ = t.Exception;
                    }
                });
            }
        };

        // 4. Configure minimum window dimensions and center window on active desktop screen
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 680;
            presenter.PreferredMinimumHeight = 500;
        }

        // Center on screen immediately with high-DPI default bounds before activation
        CenterOnScreen();

        _ = _settingsService.GetWindowSizeAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                var (width, height) = t.Result;
                if (width >= 680 && height >= 500)
                {
                    DispatcherQueue?.TryEnqueue(() => CenterOnScreen(width, height));
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

    /// <summary>
    /// Centers the application window on the current active display monitor's usable work area (excluding taskbar).
    /// </summary>
    public void CenterOnScreen(int? preferredWidth = null, int? preferredHeight = null)
    {
        try
        {
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea is null) return;

            var workArea = displayArea.WorkArea;

            // Compute ideal default dimensions if not specified or out of bounds
            int targetWidth = preferredWidth is >= 680
                ? Math.Min(preferredWidth.Value, workArea.Width)
                : Math.Min(1200, (int)(workArea.Width * 0.82));

            int targetHeight = preferredHeight is >= 500
                ? Math.Min(preferredHeight.Value, workArea.Height)
                : Math.Min(780, (int)(workArea.Height * 0.82));

            // Ensure dimensions respect absolute min limits
            targetWidth = Math.Clamp(targetWidth, 680, workArea.Width);
            targetHeight = Math.Clamp(targetHeight, 500, workArea.Height);

            // Calculate centered origin coordinates
            int x = workArea.X + (workArea.Width - targetWidth) / 2;
            int y = workArea.Y + (workArea.Height - targetHeight) / 2;

            AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, targetWidth, targetHeight));
        }
        catch
        {
            // Defensive fault isolation: fallback to OS default if DisplayArea query fails
        }
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

            if (NavView is not null)
            {
                NavView.RequestedTheme = elementTheme;
            }

            if (AppTitleBar is not null)
            {
                AppTitleBar.RequestedTheme = elementTheme;
            }

            if (ContentFrame is not null)
            {
                ContentFrame.RequestedTheme = elementTheme;
            }

            if (ContentFrame?.Content is FrameworkElement pageContent)
            {
                pageContent.RequestedTheme = elementTheme;
            }

            bool isDark = DetermineIsDark(theme, rootElement.ActualTheme);
            UpdateDwmImmersiveDarkMode(isDark);
            UpdateTitleBarButtonColors(isDark);
            UpdateBackdropAndBackground(isDark);
        }
    }

    private void UpdateBackdropAndBackground(bool isDark)
    {
        if (RootGrid is null) return;

        bool isMicaSupported = Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported();

        if (_isMicaEnabled && isMicaSupported)
        {
            SystemBackdrop = null;
            SystemBackdrop = new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt };
            // In light mode, provide a protective high-contrast tint so that when Mica samples against dark desktop or
            // fallback environments, the title bar and root chrome never degrade to solid pitch black.
            RootGrid.Background = isDark
                ? new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                : new SolidColorBrush(Windows.UI.Color.FromArgb(240, 243, 243, 243));
        }
        else
        {
            SystemBackdrop = null;
            if (Application.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out var brushObj) &&
                brushObj is Brush bgBrush)
            {
                RootGrid.Background = bgBrush;
            }
            else
            {
                RootGrid.Background = new SolidColorBrush(
                    isDark
                        ? Windows.UI.Color.FromArgb(255, 32, 32, 32)
                        : Windows.UI.Color.FromArgb(255, 243, 243, 243));
            }
        }
    }

    private void UpdateDwmImmersiveDarkMode(bool isDark)
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            if (hwnd != IntPtr.Zero)
            {
                int useDarkMode = isDark ? 1 : 0;
                int hr = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                if (hr != 0)
                {
                    _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
                }
            }
        }
        catch
        {
            // Graceful fallback on platforms without DWM support
        }
    }

    private void UpdateTitleBarButtonColors(bool isDark)
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow?.TitleBar is { } titleBar)
        {
            var foregroundColor = isDark
                ? Windows.UI.Color.FromArgb(255, 255, 255, 255)
                : Windows.UI.Color.FromArgb(255, 30, 30, 30);

            var hoverBgColor = isDark
                ? Windows.UI.Color.FromArgb(30, 255, 255, 255)
                : Windows.UI.Color.FromArgb(30, 0, 0, 0);

            var pressedBgColor = isDark
                ? Windows.UI.Color.FromArgb(45, 255, 255, 255)
                : Windows.UI.Color.FromArgb(45, 0, 0, 0);

            var inactiveForegroundColor = isDark
                ? Windows.UI.Color.FromArgb(120, 255, 255, 255)
                : Windows.UI.Color.FromArgb(120, 0, 0, 0);

            var transparent = Windows.UI.Color.FromArgb(0, 0, 0, 0);

            titleBar.BackgroundColor = transparent;
            titleBar.InactiveBackgroundColor = transparent;
            titleBar.ButtonBackgroundColor = transparent;
            titleBar.ButtonInactiveBackgroundColor = transparent;

            titleBar.ButtonForegroundColor = foregroundColor;
            titleBar.ButtonHoverForegroundColor = foregroundColor;
            titleBar.ButtonPressedForegroundColor = foregroundColor;
            titleBar.ButtonInactiveForegroundColor = inactiveForegroundColor;

            titleBar.ButtonHoverBackgroundColor = hoverBgColor;
            titleBar.ButtonPressedBackgroundColor = pressedBgColor;
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

    private void AppLauncherItem_Click(object sender, ItemClickEventArgs e)
    {
        AppLauncherFlyout?.Hide();
        if (e.ClickedItem is NavigationItemViewModel navItem)
        {
            ViewModel.NavigateItemCommand.Execute(navItem);
        }
    }
}
