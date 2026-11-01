using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;

namespace OmniForge.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettings;

    [ObservableProperty]
    public partial string AppVersion { get; set; } = "0.1.0-alpha";

    [ObservableProperty]
    public partial bool EnableMicaAlt { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusNotice))]
    public partial string StatusNotice { get; set; } = string.Empty;

    public bool HasStatusNotice => !string.IsNullOrEmpty(StatusNotice);

    public SettingsViewModel(ILocalSettingsService localSettings)
    {
        _localSettings = localSettings ?? throw new ArgumentNullException(nameof(localSettings));
    }

    [RelayCommand]
    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        EnableMicaAlt = await _localSettings.ReadSettingAsync("General.EnableMicaAlt", true, cancellationToken);
    }

    [RelayCommand]
    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        await _localSettings.SaveSettingAsync("General.EnableMicaAlt", EnableMicaAlt, cancellationToken);
        StatusNotice = "Settings saved successfully.";
    }
}
