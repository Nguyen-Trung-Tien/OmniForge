using OmniForge.Core.Contracts;

namespace OmniForge.Services;

/// <summary>
/// Bridge implementation of <see cref="IAppSettingsService"/> delegating to <see cref="ISettingsService"/>.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly ISettingsService _settingsService;

    public event EventHandler<string>? SettingChanged
    {
        add => _settingsService.SettingChanged += value;
        remove => _settingsService.SettingChanged -= value;
    }

    public AppSettingsService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        return _settingsService.GetSettingAsync(key, defaultValue, cancellationToken);
    }

    public Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        return _settingsService.SetSettingAsync(key, value, cancellationToken);
    }
}
