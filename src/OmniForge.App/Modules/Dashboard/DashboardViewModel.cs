using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Dashboard;

/// <summary>
/// Presentation wrapper for modular tools displayed on the Dashboard quick launch grid.
/// Exposes strongly-typed compiled commands eliminating DataTemplate ElementName resolution failures.
/// </summary>
public sealed class DashboardToolItemViewModel
{
    public IToolModule Module { get; }
    public string DisplayName => Module.DisplayName;
    public string Description => Module.Description;
    public string Category => Module.Category;
    public string IconGlyph => Module.IconGlyph;
    public IRelayCommand LaunchCommand { get; }

    public DashboardToolItemViewModel(IToolModule module, Action<IToolModule> onLaunch)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        LaunchCommand = new RelayCommand(() => onLaunch(module));
    }
}

public partial class DashboardViewModel : ToolViewModelBase
{
    private readonly IToolRegistryService _toolRegistry;
    private readonly INavigationService? _navigationService;

    [ObservableProperty]
    public partial string WelcomeMessage { get; set; } = "Welcome to OmniForge Utility Suite";

    [ObservableProperty]
    public partial int RegisteredModuleCount { get; set; }

    public ObservableCollection<DashboardToolItemViewModel> AvailableTools { get; } = [];

    public IReadOnlyList<IToolModule> Modules => _toolRegistry.Modules;

    public DashboardViewModel(IToolRegistryService toolRegistry, INavigationService? navigationService = null)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _navigationService = navigationService;
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        PopulateAvailableTools();

        _toolRegistry.ModulesChanged += (_, _) =>
        {
            RegisteredModuleCount = _toolRegistry.Modules.Count;
            OnPropertyChanged(nameof(Modules));
            PopulateAvailableTools();
        };
    }

    private void PopulateAvailableTools()
    {
        AvailableTools.Clear();
        foreach (var module in _toolRegistry.Modules.Where(m => m.IsEnabled))
        {
            AvailableTools.Add(new DashboardToolItemViewModel(module, LaunchModule));
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
