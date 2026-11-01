using OmniForge.Core.Contracts;

namespace OmniForge.Services;

/// <summary>
/// Bridge service implementing <see cref="IAppSettingsService"/> delegating to the singleton <see cref="ILocalSettingsService"/>.
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
    private readonly ILocalSettingsService _innerSettingsService;

    public event EventHandler<string>? SettingChanged;

    public AppSettingsService(ILocalSettingsService innerSettingsService)
    {
        _innerSettingsService = innerSettingsService ?? throw new ArgumentNullException(nameof(innerSettingsService));
        _innerSettingsService.SettingChanged += (sender, key) => SettingChanged?.Invoke(this, key);
    }

    public Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        return _innerSettingsService.ReadSettingAsync(key, defaultValue, cancellationToken);
    }

    public Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        return _innerSettingsService.SaveSettingAsync(key, value, cancellationToken);
    }
}
