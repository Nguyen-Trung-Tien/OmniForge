using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.BatchFolder;

/// <summary>
/// Modular plugin definition for the Batch Folder Creator tool.
/// Automatically discovered by ModuleAutoDiscoveryExtensions at application startup.
/// </summary>
public sealed class BatchFolderModule : IToolModule
{
    public string Id => "omniforge.batchfolder";

    public string DisplayName => "Batch Folder Creator";

    public string Title => DisplayName;

    public string Description => "Generate single or nested directory structures from flat lists or indented hierarchy templates";

    public string IconGlyph => "\uE8F4";

    public string Category => ToolCategory.FileSystem;

    public Type ViewType => typeof(BatchFolderPage);

    public Type ViewModelType => typeof(FolderCreatorViewModel);

    public int Priority => 16;

    public int Order => Priority;

    public bool IsEnabled => true;
}

