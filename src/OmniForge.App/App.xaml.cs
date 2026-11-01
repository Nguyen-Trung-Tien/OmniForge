using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniForge.App.Extensions;
using OmniForge.App.Modules.Dashboard;
using OmniForge.App.Views;
using OmniForge.Core.Contracts;
using OmniForge.Services;
using OmniForge.Services.Navigation;

namespace OmniForge.App;

/// <summary>
/// Provides application-specific behavior, hosts the DI IoC container,
/// and implements global crash telemetry and fault-isolated exception dialogs.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private readonly ILogger<App> _logger;
    private int _isErrorDialogShowing;

    /// <summary>
    /// Gets the application-wide Dependency Injection container.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Strongly-typed helper to resolve registered services from DI.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        return Current.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Resolves an object by Type from the DI container.
    /// </summary>
    public static object? GetService(Type type)
    {
        return Current.Services.GetService(type);
    }

    public App()
    {
        InitializeComponent();

        // Configure global fault isolation handlers
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Services = ConfigureServices();
        _logger = Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("OmniForge Utility Suite host initialized.");
    }

    /// <summary>
    /// Composition Root configuring all IoC services, view models, and auto-discovered modular tools.
    /// </summary>
    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 1. Core & Infrastructure Services (ToolRegistry, FileSystem, LocalSettings, SettingsService, Navigation, Logging)
        services.AddOmniForgeServices();

        // 2. Application Presentation Layer & Auto-Discovered Modules
        services.AddAppServices();

        var provider = services.BuildServiceProvider();

        // 3. Populate ToolRegistry from all auto-discovered IToolModule singletons
        var registry = provider.GetRequiredService<IToolRegistryService>();
        var discoveredModules = provider.GetServices<IToolModule>();
        registry.RegisterModules(discoveredModules);

        // 4. Pre-configure Navigation Routes
        if (provider.GetRequiredService<INavigationService>() is NavigationService navService)
        {
            navService.Configure("Settings", typeof(SettingsPage));
            navService.Configure("Dashboard", typeof(DashboardPage));
            foreach (var module in registry.Modules)
            {
                navService.Configure(module.Id, module.ViewType);
            }
        }

        return provider;
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger.LogCritical(e.Exception, "WinUI 3 Unhandled Exception: {Message}", e.Message);
        e.Handled = true;
        DisplayUnhandledErrorDialog(e.Exception, "WinUI 3 XAML Dispatcher");
    }

    private void OnCurrentDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogCritical(ex, "AppDomain Unhandled Exception: {Message}", ex.Message);
            DisplayUnhandledErrorDialog(ex, "AppDomain Unhandled Exception");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogWarning(e.Exception, "Unobserved Task Exception: {Message}", e.Exception.Message);
        e.SetObserved();
        DisplayUnhandledErrorDialog(e.Exception, "TaskScheduler Unobserved Task");
    }

    /// <summary>
    /// Displays a non-fatal, thread-safe ContentDialog with diagnostic telemetry and copy-to-clipboard functionality.
    /// </summary>
    private void DisplayUnhandledErrorDialog(Exception ex, string source)
    {
        if (_window is null) return;

        _window.DispatcherQueue?.TryEnqueue(async () =>
        {
            if (_window.Content?.XamlRoot is null) return;
            if (Interlocked.CompareExchange(ref _isErrorDialogShowing, 1, 0) != 0) return;

            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
                var telemetryReport = $"""
                    [OmniForge Crash Telemetry Report]
                    Timestamp: {timestamp}
                    Source: {source}
                    Exception Type: {ex.GetType().FullName}
                    Message: {ex.Message}
                    HResult: 0x{ex.HResult:X8}

                    --- Stack Trace ---
                    {ex.StackTrace}

                    --- Inner Exception ---
                    {ex.InnerException?.ToString() ?? "None"}
                    """;

                var dialog = new ContentDialog
                {
                    Title = "Unhandled Application Error",
                    CloseButtonText = "Dismiss",
                    PrimaryButtonText = "Copy to Clipboard",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = _window.Content.XamlRoot
                };

                var rootStack = new StackPanel { Spacing = 12, MaxWidth = 560 };

                var infoBar = new InfoBar
                {
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    IsClosable = false,
                    Title = "Fault Isolation Active",
                    Message = $"OmniForge intercepted an unhandled error from '{source}'. The application remains responsive."
                };
                rootStack.Children.Add(infoBar);

                var messageBlock = new TextBlock
                {
                    Text = ex.Message,
                    TextWrapping = TextWrapping.Wrap
                };

                if (Current.Resources.TryGetValue("BodyStrongTextBlockStyle", out var styleObj) && styleObj is Style strongStyle)
                {
                    messageBlock.Style = strongStyle;
                }

                rootStack.Children.Add(messageBlock);

                var detailsBox = new TextBox
                {
                    Text = telemetryReport,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.NoWrap,
                    Height = 180,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas, Cascadia Code, Courier New")
                };
                rootStack.Children.Add(detailsBox);

                dialog.Content = rootStack;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                        dataPackage.SetText(telemetryReport);
                        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                    }
                    catch (Exception clipEx)
                    {
                        _logger.LogWarning(clipEx, "Failed to copy telemetry report to clipboard.");
                    }
                }
            }
            catch (Exception dialogEx)
            {
                _logger.LogError(dialogEx, "Failed to present crash telemetry ContentDialog.");
            }
            finally
            {
                Interlocked.Exchange(ref _isErrorDialogShowing, 0);
            }
        });
    }
}
