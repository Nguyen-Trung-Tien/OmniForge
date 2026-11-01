using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OmniForge.Core.Contracts;

namespace OmniForge.App.Extensions;

/// <summary>
/// Provides zero-touch, convention-based automatic registration of <see cref="IToolModule"/> implementations,
/// their associated Views, ViewModels, and co-located module Services.
/// </summary>
public static class ModuleAutoDiscoveryExtensions
{
    /// <summary>
    /// Scans the specified assembly for all non-abstract <see cref="IToolModule"/> implementations and registers:
    /// 1. The <see cref="IToolModule"/> as a singleton.
    /// 2. Its <see cref="IToolModule.ViewModelType"/> and <see cref="IToolModule.ViewType"/> as transient.
    /// 3. Any co-located module service matching <c>I[Module]Service</c> -> <c>[Module]Service</c> in the same namespace.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Module auto-discovery scans loaded assembly types for IToolModule implementations.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy DynamicallyAccessedMembers", Justification = "Module discovery dynamically registers tool views, viewmodels, and module services.")]
    public static IServiceCollection AddDiscoveredToolModules(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var allTypes = assembly.GetTypes();

        var moduleTypes = allTypes
            .Where(t => typeof(IToolModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType) is not IToolModule moduleInstance || !moduleInstance.IsEnabled)
            {
                continue;
            }

            services.AddSingleton<IToolModule>(moduleInstance);
            services.AddSingleton(moduleInstance.ViewModelType);
            services.AddTransient(moduleInstance.ViewType);

            // Auto-discover co-located module services in the same namespace (e.g. ITextDiffService -> TextDiffService)
            if (!string.IsNullOrEmpty(moduleType.Namespace))
            {
                var namespaceTypes = allTypes.Where(t => t.Namespace == moduleType.Namespace).ToList();
                var serviceInterfaces = namespaceTypes.Where(t => t.IsInterface && t.Name.EndsWith("Service", StringComparison.Ordinal));

                foreach (var serviceInterface in serviceInterfaces)
                {
                    var implType = namespaceTypes.FirstOrDefault(t =>
                        t is { IsClass: true, IsAbstract: false } &&
                        serviceInterface.IsAssignableFrom(t));

                    if (implType is not null)
                    {
                        services.AddSingleton(serviceInterface, implType);
                    }
                }
            }
        }

        return services;
    }
}

