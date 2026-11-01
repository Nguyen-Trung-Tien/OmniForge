using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.TextInspector;

public sealed class TextInspectorModule : IToolModule
{
    public string Id => "TextInspector";

    public string DisplayName => "Text & Payload Inspector";

    public string Description => "JSON & XML formatting/minification, Base64/URL encoding/decoding, JWT inspector, and visual line diff.";

    public string IconGlyph => "\uE943"; // Code / document inspection glyph

    public string Category => "Developer Utilities";

    public int Priority => 25;

    public bool IsEnabled => true;

    public Type ViewType => typeof(TextInspectorPage);

    public Type ViewModelType => typeof(TextInspectorViewModel);
}
