using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Helpers;

/// <summary>
/// Dynamic XAML data binding bridge for localized strings.
/// Provides hot-reloading string resolution via indexer syntax (e.g. {Binding Source={StaticResource Loc}, Path=[Common_Save]})
/// and raises PropertyChanged for all bindings whenever the active language changes.
/// </summary>
public sealed partial class LocalizationManager : ObservableObject
{
    private ILocalizationService? _localizationService;

    public static LocalizationManager? Current { get; private set; }

    public IReadOnlyList<LanguageItem> SupportedLanguages =>
        _localizationService?.SupportedLanguages ?? [];

    public AppLanguage CurrentLanguage =>
        _localizationService?.CurrentLanguage ?? AppLanguage.English;

    public string CurrentCultureTag =>
        _localizationService?.CurrentCultureTag ?? "en-US";

    /// <summary>
    /// Indexer for XAML and code binding.
    /// Returns the localized string for <paramref name="key"/>.
    /// </summary>
    public string this[string key] => _localizationService?.GetString(key) ?? key;

    /// <summary>
    /// Parameterless constructor enabling declarative XAML instantiation inside ResourceDictionaries.
    /// Safely resolves <see cref="ILocalizationService"/> when DI is available.
    /// </summary>
    public LocalizationManager()
    {
        Current = this;
        TryAttachService();
    }

    /// <summary>
    /// DI-friendly constructor.
    /// </summary>
    public LocalizationManager(ILocalizationService localizationService)
    {
        AttachService(localizationService);
        Current = this;
    }

    public void AttachService(ILocalizationService localizationService)
    {
        if (_localizationService is not null)
        {
            _localizationService.LanguageChanged -= OnLanguageChanged;
        }

        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _localizationService.LanguageChanged += OnLanguageChanged;
        OnPropertyChanged(string.Empty);
    }

    private void TryAttachService()
    {
        try
        {
            if (App.Current?.Services is not null)
            {
                var service = App.GetService<ILocalizationService>();
                if (service is not null)
                {
                    AttachService(service);
                }
            }
        }
        catch
        {
            // Defensive fault isolation: XAML designer or pre-launch phase
        }
    }

    public string Get(string key) => _localizationService?.GetString(key) ?? key;

    public string Format(string key, params object[] args) =>
        _localizationService?.GetFormattedString(key, args) ?? key;

    [RelayCommand]
    public async Task SwitchLanguageAsync(AppLanguage language)
    {
        if (_localizationService is not null)
        {
            await _localizationService.SetLanguageAsync(language);
        }
    }

    private void OnLanguageChanged(object? sender, AppLanguage newLanguage)
    {
        // Notifies XAML that all properties including indexers have updated
        OnPropertyChanged(string.Empty);
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(CurrentCultureTag));
    }
}
