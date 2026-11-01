using System.Collections.Concurrent;
using OmniForge.Core.Contracts;

namespace OmniForge.Services;

/// <summary>
/// Thread-safe implementation of <see cref="IToolRegistryService"/> for registering and retrieving tool modules.
/// </summary>
public sealed class ToolRegistryService : IToolRegistryService
{
    private readonly ConcurrentDictionary<string, IToolModule> _modules = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? ModulesChanged;

    public IReadOnlyList<IToolModule> Modules => [.. _modules.Values.OrderBy(m => m.Category).ThenBy(m => m.Order).ThenBy(m => m.Title)];

    public IToolModule? GetModule(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _modules.TryGetValue(id, out var module);
        return module;
    }

    public void RegisterModule(IToolModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(module.Id);

        _modules.AddOrUpdate(module.Id, module, (_, _) => module);
        ModulesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RegisterModules(IEnumerable<IToolModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        bool anyAdded = false;

        foreach (var module in modules)
        {
            if (module is not null && !string.IsNullOrWhiteSpace(module.Id))
            {
                _modules.AddOrUpdate(module.Id, module, (_, _) => module);
                anyAdded = true;
            }
        }

        if (anyAdded)
        {
            ModulesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool UnregisterModule(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        bool removed = _modules.TryRemove(id, out _);
        if (removed)
        {
            ModulesChanged?.Invoke(this, EventArgs.Empty);
        }
        return removed;
    }
}
