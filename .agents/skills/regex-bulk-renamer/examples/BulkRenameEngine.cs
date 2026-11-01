using System.Text.RegularExpressions;
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.BulkRenamer;

public sealed record RenamePreviewItem(
    string SourceFullPath,
    string OriginalFileName,
    string ProposedFileName,
    string TargetFullPath,
    bool HasChange,
    bool HasConflict,
    string? ValidationError = null);

public sealed record RenameExecutionJournal(
    Guid BatchId,
    DateTimeOffset ExecutedAtUtc,
    IReadOnlyList<(string OriginalPath, string RenamedPath)> CompletedOperations);

public sealed class BulkRenameEngine
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public Result<IReadOnlyList<RenamePreviewItem>> BuildPreview(
        IReadOnlyList<string> sourceFiles,
        string searchPattern,
        string replacementTemplate,
        bool useRegex = false)
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);

        Regex? regex = null;
        if (useRegex && !string.IsNullOrEmpty(searchPattern))
        {
            try
            {
                regex = new Regex(searchPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<IReadOnlyList<RenamePreviewItem>>("Rename.InvalidRegex", ex.Message);
            }
        }

        var items = new List<RenamePreviewItem>(sourceFiles.Count);
        var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < sourceFiles.Count; i++)
        {
            var src = sourceFiles[i];
            var dir = Path.GetDirectoryName(src) ?? string.Empty;
            var parentName = Path.GetFileName(dir);
            var origName = Path.GetFileName(src);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(src);
            var ext = Path.GetExtension(src);

            string proposedBase;
            try
            {
                proposedBase = regex is not null
                    ? regex.Replace(nameWithoutExt, replacementTemplate)
                    : (!string.IsNullOrEmpty(searchPattern)
                        ? nameWithoutExt.Replace(searchPattern, replacementTemplate, StringComparison.OrdinalIgnoreCase)
                        : replacementTemplate);
            }
            catch (RegexMatchTimeoutException)
            {
                return Result.Failure<IReadOnlyList<RenamePreviewItem>>("Rename.RegexTimeout", "Regex pattern timed out (ReDoS guard).");
            }

            // Token interpolation: {name}, {index}, {parent}, {date}
            proposedBase = proposedBase
                .Replace("{name}", nameWithoutExt, StringComparison.OrdinalIgnoreCase)
                .Replace("{index}", (i + 1).ToString("D3"), StringComparison.OrdinalIgnoreCase)
                .Replace("{parent}", parentName, StringComparison.OrdinalIgnoreCase)
                .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);

            string proposedFileName = proposedBase + ext;
            string targetFullPath = Path.Combine(dir, proposedFileName);

            bool hasConflict = false;
            string? validationError = null;

            if (proposedFileName.IndexOfAny(InvalidChars) >= 0)
            {
                hasConflict = true;
                validationError = "Contains invalid Windows filename characters.";
            }
            else if (!seenTargets.Add(targetFullPath))
            {
                hasConflict = true;
                validationError = "Collision: Multiple source files resolve to this target name.";
            }

            items.Add(new RenamePreviewItem(
                src,
                origName,
                proposedFileName,
                targetFullPath,
                HasChange: !string.Equals(origName, proposedFileName, StringComparison.Ordinal),
                HasConflict: hasConflict,
                ValidationError: validationError));
        }

        return Result.Success<IReadOnlyList<RenamePreviewItem>>(items);
    }
}

