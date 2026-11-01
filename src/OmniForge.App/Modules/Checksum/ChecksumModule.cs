using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.Checksum;

public sealed class ChecksumModule : IToolModule
{
    public string Id => "Checksum";

    public string DisplayName => "File Hash & Checksum";

    public string Description => "Asynchronous cryptographic checksum computation (MD5, SHA-1, SHA-256, SHA-512) and hash verification.";

    public string IconGlyph => "\uE890"; // Ribbon or Security glyph

    public string Category => "Security & Integrity";

    public int Priority => 22;

    public bool IsEnabled => true;

    public Type ViewType => typeof(ChecksumPage);

    public Type ViewModelType => typeof(ChecksumViewModel);
}
