---
name: regex-bulk-renamer
description: Safe, ReDoS-protected Regex bulk file renaming, token interpolation ({index}, {date}, {parent}), dry-run preview collision detection, and transactional Undo Journal rollback for Phase 2.
---

# Regex Bulk File Renamer & Undo Journal Skill

Use this skill when implementing the **Batch File & Path Renamer** module (Phase 2 Roadmap). File renaming mutates user disk state and **must** enforce pre-flight collision checks, ReDoS regex timeouts, and a reversible Undo Journal.

---

## 1. Non-Negotiable Safety Laws for Bulk Renaming

1. **Dry-Run Preview First**: Never rename files on disk without first computing a deterministic `IReadOnlyList<RenamePreviewItem>` and checking for duplicate target paths or invalid Windows filename characters (`Path.GetInvalidFileNameChars()`).
2. **ReDoS Timeout Guard**: User-supplied regular expressions must always use `RegexOptions.None` (or `IgnoreCase`) with an explicit `matchTimeout: TimeSpan.FromMilliseconds(250)` to prevent catastrophic backtracking from freezing worker threads.
3. **Transactional Undo Journal**: Every executed batch rename must return an `UndoBatchRecord` (or persist to `%LocalAppData%\OmniForge\journals\`) so the user can revert the batch in reverse topological order with one click.

---

## 2. Production Reference Pattern (`IBulkRenameService`)

```csharp
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

            // Token interpolation: {name}, {index:000}, {parent}, {date}
            proposedBase = proposedBase
                .Replace("{name}", nameWithoutExt, StringComparison.OrdinalIgnoreCase)
                .Replace("{index}", (i + 1).ToString("D3"), StringComparison.OrdinalIgnoreCase)
                .Replace("{parent}", parentName, StringComparison.OrdinalIgnoreCase)
                .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);

            var proposedFileName = proposedBase.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                ? proposedBase
                : proposedBase + ext;

            var targetFullPath = Path.Combine(dir, proposedFileName);
            bool hasInvalidChars = proposedFileName.IndexOfAny(InvalidChars) >= 0;
            bool isDuplicate = !seenTargets.Add(targetFullPath);

            items.Add(new RenamePreviewItem(
                src,
                origName,
                proposedFileName,
                targetFullPath,
                HasChange: !string.Equals(src, targetFullPath, StringComparison.OrdinalIgnoreCase),
                HasConflict: isDuplicate || hasInvalidChars,
                ValidationError: hasInvalidChars ? "Contains invalid filename characters." : (isDuplicate ? "Target filename collision." : null)));
        }

        return Result.Success<IReadOnlyList<RenamePreviewItem>>(items);
    }

    public async Task<Result<RenameExecutionJournal>> ExecuteBatchAsync(
        IReadOnlyList<RenamePreviewItem> previewItems,
        CancellationToken cancellationToken = default)
    {
        if (previewItems.Any(p => p.HasConflict))
            return Result.Failure<RenameExecutionJournal>("Rename.ConflictDetected", "Resolve all filename collisions before executing.");

        return await Task.Run(() =>
        {
            var completed = new List<(string OriginalPath, string RenamedPath)>();
            try
            {
                foreach (var item in previewItems.Where(p => p.HasChange))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(item.SourceFullPath, item.TargetFullPath, overwrite: false);
                    completed.Add((item.SourceFullPath, item.TargetFullPath));
                }

                return Result.Success(new RenameExecutionJournal(Guid.NewGuid(), DateTimeOffset.UtcNow, completed));
            }
            catch (Exception ex)
            {
                // Automatic rollback on partial failure
                for (int i = completed.Count - 1; i >= 0; i--)
                {
                    try { File.Move(completed[i].RenamedPath, completed[i].OriginalPath, overwrite: false); }
                    catch { /* Best effort rollback */ }
                }
                return Result.Failure<RenameExecutionJournal>("Rename.ExecutionFailed", $"Batch aborted and rolled back: {ex.Message}");
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

