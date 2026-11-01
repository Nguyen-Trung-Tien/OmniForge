using OmniForge.Core.Common;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Decoupled abstraction for reading and writing system clipboard text so ViewModels remain 100% unit-testable.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies the specified text to the system clipboard.
    /// </summary>
    Result CopyText(string text);

    /// <summary>
    /// Asynchronously reads plain text from the system clipboard.
    /// </summary>
    Task<Result<string>> GetTextAsync(CancellationToken cancellationToken = default);
}

