using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Dashboard;

public partial class DashboardViewModel : ToolViewModelBase
{
    private readonly IToolRegistryService _toolRegistry;

    [ObservableProperty]
    public partial string WelcomeMessage { get; set; } = "Welcome to OmniForge Utility Suite";

    [ObservableProperty]
    public partial int RegisteredModuleCount { get; set; }

    public DashboardViewModel(IToolRegistryService toolRegistry)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        _toolRegistry.ModulesChanged += (_, _) => RegisteredModuleCount = _toolRegistry.Modules.Count;
    }

    [RelayCommand]
    private void RefreshStatistics()
    {
        RegisteredModuleCount = _toolRegistry.Modules.Count;
        StatusMessage = $"Refreshed ({RegisteredModuleCount} active modules).";
    }
}
