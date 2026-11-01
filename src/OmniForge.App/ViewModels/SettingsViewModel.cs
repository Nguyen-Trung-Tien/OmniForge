using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private bool _isInitialLoading = true;

    [ObservableProperty]
    public partial string AppVersion { get; set; } = "0.1.0-alpha";

    [ObservableProperty]
    public partial bool EnableMicaAlt { get; set; } = true;

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = "Default";

    public IReadOnlyList<string> ThemeOptions { get; } = ["Default", "Light", "Dark"];

    public IReadOnlyList<LanguageItem> SupportedLanguages => _localizationService.SupportedLanguages;

    [ObservableProperty]
    public partial LanguageItem? SelectedLanguage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusNotice))]
    public partial string StatusNotice { get; set; } = string.Empty;

    public bool HasStatusNotice => !string.IsNullOrEmpty(StatusNotice);

    public SettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        _localizationService.LanguageChanged += OnLanguageChanged;

        _ = LoadSettingsAsync(CancellationToken.None);
    }

    async partial void OnSelectedThemeChanged(string value)
    {
        if (!_isInitialLoading && !string.IsNullOrWhiteSpace(value))
        {
            await _settingsService.SetThemeAsync(value);
            StatusNotice = _localizationService.GetFormattedString(LocalizationKeys.Settings_ThemeUpdatedLive, value);
        }
    }

    async partial void OnSelectedLanguageChanged(LanguageItem? value)
    {
        if (!_isInitialLoading && value is not null && value.Language != _localizationService.CurrentLanguage)
        {
            await _localizationService.SetLanguageAsync(value.Language);
            StatusNotice = _localizationService.GetString(LocalizationKeys.Settings_SavedSuccess);
        }
    }

    private void OnLanguageChanged(object? sender, AppLanguage newLanguage)
    {
        SelectedLanguage = _localizationService.SupportedLanguages.FirstOrDefault(l => l.Language == newLanguage);
        OnPropertyChanged(nameof(SupportedLanguages));
    }

    [RelayCommand]
    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _isInitialLoading = true;
            EnableMicaAlt = await _settingsService.GetSettingAsync("General.EnableMicaAlt", true, cancellationToken);
            SelectedTheme = await _settingsService.GetThemeAsync(cancellationToken);
            SelectedLanguage = _localizationService.SupportedLanguages.FirstOrDefault(l => l.Language == _localizationService.CurrentLanguage);
        }
        finally
        {
            _isInitialLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        await _settingsService.SetSettingAsync("General.EnableMicaAlt", EnableMicaAlt, cancellationToken);
        await _settingsService.SetThemeAsync(SelectedTheme, cancellationToken);
        if (SelectedLanguage is not null)
        {
            await _localizationService.SetLanguageAsync(SelectedLanguage.Language, cancellationToken);
        }
        StatusNotice = _localizationService.GetString(LocalizationKeys.Settings_SavedSuccess);
    }
}
