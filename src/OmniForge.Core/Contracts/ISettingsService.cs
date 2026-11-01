namespace OmniForge.Core.Contracts;

/// <summary>
/// Service contract providing strongly typed application configuration, theme preferences,
/// window state persistence, and custom user templates.
/// </summary>
public interface ISettingsService : IAppSettingsService
{
    /// <summary>
    /// Gets the user's selected application theme ("Light", "Dark", or "Default").
    /// </summary>
    Task<string> GetThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the user's selected application theme.
    /// </summary>
    Task SetThemeAsync(string theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the identifier of the last opened utility module.
    /// </summary>
    Task<string?> GetLastOpenedModuleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the identifier of the last opened utility module.
    /// </summary>
    Task SetLastOpenedModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the persisted window dimensions (Width, Height).
    /// </summary>
    Task<(int Width, int Height)> GetWindowSizeAsync(int defaultWidth = 1200, int defaultHeight = 800, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the main window dimensions.
    /// </summary>
    Task SetWindowSizeAsync(int width, int height, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all custom user templates stored under a given category.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetUserTemplatesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a custom user template under a given category.
    /// </summary>
    Task SaveUserTemplateAsync(string category, string templateName, string templateContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom user template under a given category.
    /// </summary>
    Task DeleteUserTemplateAsync(string category, string templateName, CancellationToken cancellationToken = default);
}

