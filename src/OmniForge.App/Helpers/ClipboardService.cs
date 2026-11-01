using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using Windows.ApplicationModel.DataTransfer;

namespace OmniForge.App.Helpers;

/// <summary>
/// WinUI 3 implementation of <see cref="IClipboardService"/> wrapping <see cref="Clipboard"/>.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    public Result CopyText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Result.Failure("Clipboard.Empty", "Cannot copy empty text to clipboard.");
        }

        try
        {
            var package = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            package.SetText(text);
            Clipboard.SetContent(package);
            Clipboard.Flush();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("Clipboard.CopyFailed", ex.Message);
        }
    }

    public async Task<Result<string>> GetTextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text))
            {
                return Result.Failure<string>("Clipboard.NoText", "Clipboard does not contain plain text.");
            }

            var text = await content.GetTextAsync().AsTask(cancellationToken);
            return Result.Success(text ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<string>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Clipboard.ReadFailed", ex.Message);
        }
    }
}

