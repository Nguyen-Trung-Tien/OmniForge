using OmniForge.Core.Contracts;

namespace OmniForge.Services;

/// <summary>
/// Production settings service implementing <see cref="ISettingsService"/> and <see cref="IAppSettingsService"/>,
/// backed by persistent JSON storage in %LocalAppData%/OmniForge/settings.json.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    public const string ThemeSettingKey = "Theme";
    public const string LastOpenedModuleKey = "LastOpenedModule";
    public const string WindowWidthKey = "Window.Width";
    public const string WindowHeightKey = "Window.Height";
    public const string TemplatesKeyPrefix = "Templates.";

    private readonly ILocalSettingsService _localSettings;

    public event EventHandler<string>? SettingChanged;

    public SettingsService(ILocalSettingsService localSettings)
    {
        _localSettings = localSettings ?? throw new ArgumentNullException(nameof(localSettings));
        _localSettings.SettingChanged += OnInnerSettingChanged;
    }

    public Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        return _localSettings.ReadSettingAsync(key, defaultValue, cancellationToken);
    }

    public Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        return _localSettings.SaveSettingAsync(key, value, cancellationToken);
    }

    public Task<string> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        return _localSettings.ReadSettingAsync(ThemeSettingKey, "Default", cancellationToken);
    }

    public Task SetThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        return _localSettings.SaveSettingAsync(ThemeSettingKey, theme, cancellationToken);
    }

    public Task<string?> GetLastOpenedModuleAsync(CancellationToken cancellationToken = default)
    {
        return _localSettings.ReadSettingAsync<string?>(LastOpenedModuleKey, null, cancellationToken);
    }

    public Task SetLastOpenedModuleAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        return _localSettings.SaveSettingAsync(LastOpenedModuleKey, moduleId, cancellationToken);
    }

    public async Task<(int Width, int Height)> GetWindowSizeAsync(int defaultWidth = 1200, int defaultHeight = 800, CancellationToken cancellationToken = default)
    {
        var width = await _localSettings.ReadSettingAsync(WindowWidthKey, defaultWidth, cancellationToken).ConfigureAwait(false);
        var height = await _localSettings.ReadSettingAsync(WindowHeightKey, defaultHeight, cancellationToken).ConfigureAwait(false);
        return (width, height);
    }

    public async Task SetWindowSizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        if (width > 300 && height > 200)
        {
            await _localSettings.SaveSettingAsync(WindowWidthKey, width, cancellationToken).ConfigureAwait(false);
            await _localSettings.SaveSettingAsync(WindowHeightKey, height, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> GetUserTemplatesAsync(string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        var key = TemplatesKeyPrefix + category;
        var templates = await _localSettings.ReadSettingAsync<Dictionary<string, string>>(key, [], cancellationToken).ConfigureAwait(false);
        return templates ?? new Dictionary<string, string>();
    }

    public async Task SaveUserTemplateAsync(string category, string templateName, string templateContent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        var key = TemplatesKeyPrefix + category;
        var templates = await _localSettings.ReadSettingAsync<Dictionary<string, string>>(key, [], cancellationToken).ConfigureAwait(false) ?? [];
        templates[templateName] = templateContent;
        await _localSettings.SaveSettingAsync(key, templates, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteUserTemplateAsync(string category, string templateName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        var key = TemplatesKeyPrefix + category;
        var templates = await _localSettings.ReadSettingAsync<Dictionary<string, string>>(key, [], cancellationToken).ConfigureAwait(false);
        if (templates is not null && templates.Remove(templateName))
        {
            await _localSettings.SaveSettingAsync(key, templates, cancellationToken).ConfigureAwait(false);
        }
    }

    private void OnInnerSettingChanged(object? sender, string key)
    {
        SettingChanged?.Invoke(this, key);
    }
}

