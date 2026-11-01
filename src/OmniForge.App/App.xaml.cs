using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using OmniForge.App.Extensions;
using OmniForge.App.Modules.Dashboard;
using OmniForge.App.Modules.FileTools;
using OmniForge.App.Views;
using OmniForge.Core.Contracts;
using OmniForge.Services;
using OmniForge.Services.Navigation;

namespace OmniForge.App;

/// <summary>
/// Provides application-specific behavior and hosts the Microsoft.Extensions.DependencyInjection IoC container.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private readonly ILogger<App> _logger;

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
    /// Composition Root configuring all IoC services, view models, and modular tools.
    /// </summary>
    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 1. Core & Infrastructure Services (ToolRegistry, FileSystem, LocalSettings, Navigation, Logging)
        services.AddOmniForgeServices();

        // 2. Application Presentation Layer (Dispatcher, ViewModels, Views, Shell)
        services.AddAppServices();

        var provider = services.BuildServiceProvider();

        // 3. Register Built-in Modular Utility Plugins
        var registry = provider.GetRequiredService<IToolRegistryService>();
        registry.RegisterModule(new DashboardModule());
        registry.RegisterModule(new FileToolsModule());

        // 4. Pre-configure Navigation Routes
        if (provider.GetRequiredService<INavigationService>() is NavigationService navService)
        {
            navService.Configure("Settings", typeof(SettingsPage));
            navService.Configure("Dashboard", typeof(DashboardPage));
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
        // Prevent immediate termination when error can be isolated
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogCritical(ex, "AppDomain Unhandled Exception: {Message}", ex.Message);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogWarning(e.Exception, "Unobserved Task Exception: {Message}", e.Exception.Message);
        e.SetObserved();
    }
}
