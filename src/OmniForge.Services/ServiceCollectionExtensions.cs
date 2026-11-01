using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniForge.Core.Contracts;
using OmniForge.Services.Logging;
using OmniForge.Services.Navigation;
using OmniForge.Services.Settings;

namespace OmniForge.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OmniForge core infrastructure services (Navigation, Settings, FileSystem, ToolRegistry, Logging) in the DI container.
    /// </summary>
    public static IServiceCollection AddOmniForgeServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new FileLoggerProvider());
        });

        services.AddSingleton<IToolRegistryService, ToolRegistryService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ISettingsService>(sp => sp.GetRequiredService<SettingsService>());
        services.AddSingleton<IAppSettingsService>(sp => sp.GetRequiredService<SettingsService>());
        services.AddSingleton<AppSettingsService>();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        return services;
    }
}
