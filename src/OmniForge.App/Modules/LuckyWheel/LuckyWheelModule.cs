using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.LuckyWheel;

/// <summary>
/// Modular plugin definition for the Lucky Wheel decision mini-tool.
/// Automatically discovered by ModuleAutoDiscoveryExtensions at application startup.
/// </summary>
public sealed class LuckyWheelModule : IToolModule
{
    public string Id => "omniforge.luckywheel";

    public string DisplayName => "Lucky Wheel";

    public string Title => DisplayName;

    public string Description => "High-performance 60/120 FPS interactive physics decision wheel with custom weights and Win2D rendering";

    public string IconGlyph => "\uE735"; // Star / Favorite / Lucky Icon

    public string Category => ToolCategory.Developer;

    public Type ViewType => typeof(LuckyWheelPage);

    public Type ViewModelType => typeof(LuckyWheelViewModel);

    public int Priority => 30;

    public int Order => Priority;

    public bool IsEnabled => true;
}

