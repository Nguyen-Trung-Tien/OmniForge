using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Dashboard;

public sealed class DashboardModule : IToolModule
{
    public string Id => "omniforge.dashboard";
    public string DisplayName => "Dashboard";
    public string Title => DisplayName;
    public string Description => "System overview and utility quick-launch";
    public string IconGlyph => "\uE80F";
    public string Category => ToolCategory.Dashboard;
    public Type ViewType => typeof(DashboardPage);
    public Type ViewModelType => typeof(DashboardViewModel);
    public int Priority => 0;
    public int Order => Priority;
    public bool IsEnabled => true;
}
