using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.BatchRename;

/// <summary>
/// Modular plugin definition for the Batch Rename tool.
/// Automatically discovered by ModuleAutoDiscoveryExtensions at startup.
/// </summary>
public sealed class BatchRenameModule : IToolModule
{
    public string Id => "omniforge.batchrename";

    public string DisplayName => "Batch Rename";

    public string Title => DisplayName;

    public string Description => "Chained transformation rules, live collision detection, and non-blocking batch renaming";

    public string IconGlyph => "\uE8AC";

    public string Category => ToolCategory.FileSystem;

    public Type ViewType => typeof(BatchRenamePage);

    public Type ViewModelType => typeof(BatchRenameViewModel);

    public int Priority => 15;

    public int Order => Priority;

    public bool IsEnabled => true;
}

