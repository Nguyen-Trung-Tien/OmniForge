using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Dashboard;

public partial class DashboardViewModel : ToolViewModelBase
{
    private readonly IToolRegistryService _toolRegistry;
    private readonly INavigationService? _navigationService;

    [ObservableProperty]
    public partial string WelcomeMessage { get; set; } = "Welcome to OmniForge Utility Suite";

    [ObservableProperty]
    public partial int RegisteredModuleCount { get; set; }

    public IReadOnlyList<IToolModule> Modules => _toolRegistry.Modules;

    public DashboardViewModel(IToolRegistryService toolRegistry, INavigationService? navigationService = null)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _navigationService = navigationService;
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        _toolRegistry.ModulesChanged += (_, _) =>
        {
            RegisteredModuleCount = _toolRegistry.Modules.Count;
            OnPropertyChanged(nameof(Modules));
        };
    }

    [RelayCommand]
    private void RefreshStatistics()
    {
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        OnPropertyChanged(nameof(Modules));
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
