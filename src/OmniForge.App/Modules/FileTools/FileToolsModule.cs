using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.FileTools;

public sealed class FileToolsModule : IToolModule
{
    public string Id => "omniforge.filetools";
    public string DisplayName => "File Inspector";
    public string Title => DisplayName;
    public string Description => "Non-blocking file inspection and storage utilities";
    public string IconGlyph => "\uE8B7";
    public string Category => ToolCategory.FileSystem;
    public Type ViewType => typeof(FileToolsPage);
    public Type ViewModelType => typeof(FileToolsViewModel);
    public int Priority => 10;
    public int Order => Priority;
    public bool IsEnabled => true;
}
