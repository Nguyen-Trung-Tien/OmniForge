using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;

namespace OmniForge.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    public partial string AppVersion { get; set; } = "0.1.0-alpha";

    [ObservableProperty]
    public partial bool EnableMicaAlt { get; set; } = true;

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = "Default";

    public IReadOnlyList<string> ThemeOptions { get; } = ["Default", "Light", "Dark"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusNotice))]
    public partial string StatusNotice { get; set; } = string.Empty;

    public bool HasStatusNotice => !string.IsNullOrEmpty(StatusNotice);

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _ = LoadSettingsAsync(CancellationToken.None);
    }

    async partial void OnSelectedThemeChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            await _settingsService.SetThemeAsync(value);
            StatusNotice = $"Theme updated to '{value}' (applied live).";
        }
    }

    [RelayCommand]
    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        EnableMicaAlt = await _settingsService.GetSettingAsync("General.EnableMicaAlt", true, cancellationToken);
        SelectedTheme = await _settingsService.GetThemeAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        await _settingsService.SetSettingAsync("General.EnableMicaAlt", EnableMicaAlt, cancellationToken);
        await _settingsService.SetThemeAsync(SelectedTheme, cancellationToken);
        StatusNotice = "Preferences saved successfully.";
    }
}
