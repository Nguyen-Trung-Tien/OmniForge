using Microsoft.Extensions.DependencyInjection;
using OmniForge.App.Helpers;
using OmniForge.App.Modules.Dashboard;
using OmniForge.App.Modules.FileTools;
using OmniForge.App.ViewModels;
using OmniForge.App.Views;
using OmniForge.Core.Contracts;

namespace OmniForge.App.Extensions;

/// <summary>
/// Extension methods providing clean, modular dependency injection registrations for OmniForge presentation layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all presentation components: Dispatcher, ViewModels, Pages, and Shell Window.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // 1. UI Thread Dispatcher Abstraction
        services.AddSingleton<DispatcherService>();
        services.AddSingleton<IDispatcherService>(sp => sp.GetRequiredService<DispatcherService>());

        // 2. Shell & Feature ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<FileToolsViewModel>();
        services.AddTransient<SettingsViewModel>();

        // 3. Shell & Feature Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<FileToolsPage>();
        services.AddTransient<SettingsPage>();

        return services;
    }
}
