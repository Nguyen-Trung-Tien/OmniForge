using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Dashboard;

/// <summary>
/// Presentation wrapper for modular tools displayed on the Dashboard quick launch grid.
/// Exposes strongly-typed compiled commands and localized titles/descriptions.
/// </summary>
public sealed class DashboardToolItemViewModel
{
    private readonly ILocalizationService? _loc;
    public IToolModule Module { get; }

    public string DisplayName =>
        _loc?.GetString($"{Module.Id}.Title") is { Length: > 0 } loc && !loc.StartsWith('[') ? loc : Module.DisplayName;

    public string Description =>
        _loc?.GetString($"{Module.Id}.Desc") is { Length: > 0 } loc && !loc.StartsWith('[') ? loc : Module.Description;

    public string Category => Module.Category;
    public string IconGlyph => Module.IconGlyph;
    public string LaunchText => _loc?.GetString(LocalizationKeys.Dashboard_Launch) ?? "Launch";
    public IRelayCommand LaunchCommand { get; }

    public DashboardToolItemViewModel(IToolModule module, Action<IToolModule> onLaunch, ILocalizationService? loc = null)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        _loc = loc;
        LaunchCommand = new RelayCommand(() => onLaunch(module));
    }
}

public partial class DashboardViewModel : ToolViewModelBase
{
    private readonly IToolRegistryService _toolRegistry;
    private readonly INavigationService? _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty]
    public partial string PageTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PageSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SystemStatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SystemStatusSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WelcomeMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RegisteredToolsLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighlightsTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighlightsSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Highlight1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Highlight2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Highlight3 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvailableUtilitiesTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvailableUtilitiesSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int RegisteredModuleCount { get; set; }

    public ObservableCollection<DashboardToolItemViewModel> AvailableTools { get; } = [];

    public IReadOnlyList<IToolModule> Modules => _toolRegistry.Modules;

    public DashboardViewModel(
        IToolRegistryService toolRegistry,
        ILocalizationService localizationService,
        IDispatcherService dispatcherService,
        INavigationService? navigationService = null)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        _navigationService = navigationService;

        UpdateLocalizedStrings();
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        PopulateAvailableTools();

        _toolRegistry.ModulesChanged += (_, _) => _dispatcherService.TryEnqueue(() =>
        {
            RegisteredModuleCount = _toolRegistry.Modules.Count;
            OnPropertyChanged(nameof(Modules));
            PopulateAvailableTools();
        });

        _localizationService.LanguageChanged += (_, _) => _dispatcherService.TryEnqueue(() =>
        {
            UpdateLocalizedStrings();
            PopulateAvailableTools();
        });
    }

    private void UpdateLocalizedStrings()
    {
        PageTitle = _localizationService.GetString(LocalizationKeys.Dashboard_Title);
        PageSubtitle = _localizationService.GetString(LocalizationKeys.Dashboard_Subtitle);
        SystemStatusTitle = _localizationService.GetString(LocalizationKeys.Dashboard_Status_Title);
        SystemStatusSubtitle = _localizationService.GetString(LocalizationKeys.Dashboard_Status_Subtitle);
        WelcomeMessage = _localizationService.GetString(LocalizationKeys.Dashboard_Welcome);
        RegisteredToolsLabel = _localizationService.GetString(LocalizationKeys.Dashboard_RegisteredTools);
        HighlightsTitle = _localizationService.GetString(LocalizationKeys.Dashboard_Highlights_Title);
        HighlightsSubtitle = _localizationService.GetString(LocalizationKeys.Dashboard_Highlights_Subtitle);
        Highlight1 = _localizationService.GetString(LocalizationKeys.Dashboard_Highlight1);
        Highlight2 = _localizationService.GetString(LocalizationKeys.Dashboard_Highlight2);
        Highlight3 = _localizationService.GetString(LocalizationKeys.Dashboard_Highlight3);
        AvailableUtilitiesTitle = _localizationService.GetString(LocalizationKeys.Dashboard_Available_Title);
        AvailableUtilitiesSubtitle = _localizationService.GetString(LocalizationKeys.Dashboard_Available_Subtitle);
    }

    private void PopulateAvailableTools()
    {
        AvailableTools.Clear();
        foreach (var module in _toolRegistry.Modules.Where(m => m.IsEnabled))
        {
            AvailableTools.Add(new DashboardToolItemViewModel(module, LaunchModule, _localizationService));
        }
    }

    [RelayCommand]
    private void RefreshStatistics()
    {
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        OnPropertyChanged(nameof(Modules));
        PopulateAvailableTools();
        StatusMessage = $"Refreshed ({RegisteredModuleCount} active modules).";
    }

    [RelayCommand]
    private void LaunchModule(IToolModule? module)
    {
        if (module is not null && _navigationService is not null)
        {
            _navigationService.NavigateTo(module.ViewType);
        }
    }
}
