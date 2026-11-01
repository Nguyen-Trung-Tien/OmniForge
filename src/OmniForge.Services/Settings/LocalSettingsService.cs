using System.Collections.Concurrent;
using System.Text.Json;
using OmniForge.Core.Contracts;

namespace OmniForge.Services.Settings;

/// <summary>
/// Provides persistent asynchronous application settings storage backed by JSON in %LocalAppData%.
/// </summary>
public sealed class LocalSettingsService : ILocalSettingsService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentDictionary<string, JsonElement> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLoaded;

    public event EventHandler<string>? SettingChanged;

    public LocalSettingsService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniForge");

        _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
    }

    public LocalSettingsService(string customSettingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customSettingsFilePath);
        _settingsFilePath = customSettingsFilePath;
    }

    public async Task<T> ReadSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (_cache.TryGetValue(key, out var element))
        {
            try
            {
                var deserialized = element.Deserialize<T>();
                if (deserialized is not null)
                {
                    return deserialized;
                }
            }
            catch (JsonException)
            {
                // Return defaultValue if JSON schema has diverged
            }
        }

        return defaultValue;
    }

    public async Task SaveSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        var element = JsonSerializer.SerializeToElement(value);
        _cache[key] = element;

        await FlushToFileAsync(cancellationToken).ConfigureAwait(false);
        SettingChanged?.Invoke(this, key);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_isLoaded) return;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isLoaded) return;

            if (File.Exists(_settingsFilePath))
            {
                await using var stream = new FileStream(
                    _settingsFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
                    stream,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (dict is not null)
                {
                    foreach (var (k, v) in dict)
                    {
                        _cache[k] = v;
                    }
                }
            }

            _isLoaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task FlushToFileAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var snapshot = new Dictionary<string, JsonElement>(_cache, StringComparer.OrdinalIgnoreCase);
            var tempFilePath = _settingsFilePath + ".tmp";

            await using (var stream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFilePath, _settingsFilePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
