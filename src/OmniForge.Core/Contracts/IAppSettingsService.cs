namespace OmniForge.Core.Contracts;

/// <summary>
/// Service for loading and persisting strongly typed application configuration and user preferences.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Retrieves a setting value asynchronously, returning defaultValue if not found.
    /// </summary>
    Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a setting value asynchronously.
    /// </summary>
    Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event triggered when an application setting value changes.
    /// </summary>
    event EventHandler<string>? SettingChanged;
}

