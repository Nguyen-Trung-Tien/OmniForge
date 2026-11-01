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
    private readonly IDispatcherService _dispatcherService;
    private bool _isInitialLoading = true;
    private bool _isUpdatingSelection;

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

    [ObservableProperty]
    public partial string TitleText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SubtitleText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LanguageTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LanguageSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LanguageLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LanguageNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AppearanceTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AppearanceSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ThemeLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BackdropLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VersionLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SavePreferencesLabel { get; set; } = string.Empty;

    public SettingsViewModel(
        ISettingsService settingsService,
        ILocalizationService localizationService,
        IDispatcherService dispatcherService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

        _localizationService.LanguageChanged += OnLanguageChanged;
        UpdateLocalizedStrings();

        _ = LoadSettingsAsync(CancellationToken.None);
    }

    private void UpdateLocalizedStrings()
    {
        TitleText = _localizationService.GetString(LocalizationKeys.Settings_Title);
        SubtitleText = _localizationService.GetString(LocalizationKeys.Settings_Subtitle);
        LanguageTitle = _localizationService.GetString(LocalizationKeys.Settings_Language_Title);
        LanguageSubtitle = _localizationService.GetString(LocalizationKeys.Settings_Language_Subtitle);
        LanguageLabel = _localizationService.GetString(LocalizationKeys.Settings_Language_Label);
        LanguageNote = _localizationService.GetString(LocalizationKeys.Settings_Language_Note);
        AppearanceTitle = _localizationService.GetString(LocalizationKeys.Settings_Appearance_Title);
        AppearanceSubtitle = _localizationService.GetString(LocalizationKeys.Settings_Appearance_Subtitle);
        ThemeLabel = _localizationService.GetString(LocalizationKeys.Settings_Theme);
        BackdropLabel = _localizationService.GetString(LocalizationKeys.Settings_MicaBackdrop);
        VersionLabel = _localizationService.GetString(LocalizationKeys.Common_Version);
        SavePreferencesLabel = _localizationService.GetString(LocalizationKeys.Settings_SavePreferences);
    }

    async partial void OnSelectedThemeChanged(string value)
    {
        if (!_isInitialLoading && !string.IsNullOrWhiteSpace(value))
        {
            await _settingsService.SetThemeAsync(value);
            StatusNotice = _localizationService.GetFormattedString(LocalizationKeys.Settings_ThemeUpdatedLive, value);
        }
    }

    async partial void OnEnableMicaAltChanged(bool value)
    {
        if (!_isInitialLoading)
        {
            await _settingsService.SetSettingAsync("General.EnableMicaAlt", value);
        }
    }

    async partial void OnSelectedLanguageChanged(LanguageItem? value)
    {
        if (!_isInitialLoading && !_isUpdatingSelection && value is not null && value.Language != _localizationService.CurrentLanguage)
        {
            await _localizationService.SetLanguageAsync(value.Language);
            StatusNotice = _localizationService.GetString(LocalizationKeys.Settings_SavedSuccess);
        }
    }

    private void OnLanguageChanged(object? sender, AppLanguage newLanguage)
    {
        _dispatcherService.TryEnqueue(() =>
        {
            _isUpdatingSelection = true;
            try
            {
                SelectedLanguage = _localizationService.SupportedLanguages.FirstOrDefault(l => l.Language == newLanguage);
                UpdateLocalizedStrings();
                OnPropertyChanged(nameof(SupportedLanguages));
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        });
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
