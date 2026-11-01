using Microsoft.Extensions.DependencyInjection;
using OmniForge.App.Helpers;
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
    /// Registers all presentation components: Dispatcher, Shell ViewModels/Views, and auto-discovered <see cref="IToolModule"/> plugins.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // 1. UI Thread Dispatcher & Native Interop Services (Clipboard, File/Folder Pickers, Localization)
        services.AddSingleton<DispatcherService>();
        services.AddSingleton<IDispatcherService>(sp => sp.GetRequiredService<DispatcherService>());
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<LocalizationManager>();

        // 2. Shell Window & Core Pages
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsPage>();

        // 3. Auto-discover and register all IToolModule plugins, their ViewModels, Views, and Module Services
        services.AddDiscoveredToolModules(typeof(App).Assembly);

        return services;
    }
}
