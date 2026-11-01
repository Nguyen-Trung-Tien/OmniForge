namespace OmniForge.Core.Contracts;

/// <summary>
/// Service responsible for managing, registering, and retrieving modular tools.
/// </summary>
public interface IToolRegistryService
{
    /// <summary>
    /// Gets all registered tool modules sorted by category, priority, and title.
    /// </summary>
    IReadOnlyList<IToolModule> Modules { get; }

    /// <summary>
    /// Event triggered when modules are registered or dynamically modified.
    /// </summary>
    event EventHandler? ModulesChanged;

    /// <summary>
    /// Retrieves a module by its unique identifier.
    /// </summary>
    /// <param name="id">The tool identifier.</param>
    /// <returns>The matching <see cref="IToolModule"/> or null if not found.</returns>
    IToolModule? GetModule(string id);

    /// <summary>
    /// Registers a module instance into the registry.
    /// </summary>
    /// <param name="module">The tool module instance.</param>
    void RegisterModule(IToolModule module);

    /// <summary>
    /// Registers multiple module instances into the registry.
    /// </summary>
    /// <param name="modules">The tool module collection.</param>
    void RegisterModules(IEnumerable<IToolModule> modules);

    /// <summary>
    /// Unregisters a module by its identifier.
    /// </summary>
    /// <param name="id">The tool identifier.</param>
    /// <returns>True if the module was removed; otherwise false.</returns>
    bool UnregisterModule(string id);
}

