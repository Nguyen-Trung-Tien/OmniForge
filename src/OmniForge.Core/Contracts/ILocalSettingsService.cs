namespace OmniForge.Core.Contracts;

/// <summary>
/// Defines the asynchronous contract for reading and writing persistent application settings.
/// </summary>
public interface ILocalSettingsService
{
    /// <summary>
    /// Event raised when a setting value changes.
    /// </summary>
    event EventHandler<string>? SettingChanged;

    /// <summary>
    /// Asynchronously retrieves a setting value by key, or returns the default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The unique key identifying the setting.</param>
    /// <param name="defaultValue">The fallback value when the key does not exist.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>The stored setting value or default.</returns>
    Task<T> ReadSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously persists a setting value by key.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The unique key identifying the setting.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    Task SaveSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}

