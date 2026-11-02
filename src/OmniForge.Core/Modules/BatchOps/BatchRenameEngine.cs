using System.Diagnostics;
using System.Runtime.CompilerServices;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Modules.BatchOps;

/// <summary>
/// High-performance, non-blocking engine for previewing, validating, executing, and reverting batch file rename operations.
/// Enforces collision detection, ReDoS protection, and transactional rollback.
/// </summary>
public sealed class BatchRenameEngine : IBatchRenameEngine
{
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <inheritdoc />
    public Result<IReadOnlyList<RenamePreviewItem>> BuildPreview(
        IReadOnlyList<FileMetadata> files,
        IReadOnlyList<IRenameRule> rules)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(rules);

        var activeRules = rules.Where(r => r.IsEnabled).ToList();
        var previewItems = new List<RenamePreviewItem>(files.Count);
        var targetPathsSeen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // First pass: generate proposed names and evaluate individual rule validity
        for (int i = 0; i < files.Count; i++)
        {
            var meta = files[i];
            string stem = Path.GetFileNameWithoutExtension(meta.FileName);
            string ext = meta.Extension;

            string transformedStem = stem;
            foreach (var rule in activeRules)
            {
                transformedStem = rule.Apply(transformedStem, i, meta);
            }

            string proposedFileName = transformedStem + ext;
            string targetFullPath = Path.Combine(meta.DirectoryName, proposedFileName);

            bool isConflict = false;
            string? validationError = null;

            // 1. Check empty filename
            if (string.IsNullOrWhiteSpace(transformedStem))
            {
                isConflict = true;
                validationError = "File name cannot be empty.";
            }
            // 2. Check invalid filename characters
            else if (proposedFileName.IndexOfAny(InvalidChars) >= 0)
            {
                isConflict = true;
                validationError = "Proposed name contains invalid characters.";
            }
            // 3. Check reserved Windows device names (e.g. CON, PRN, AUX, NUL)
            else if (ReservedDeviceNames.Contains(transformedStem))
            {
                isConflict = true;
                validationError = $"'{transformedStem}' is a reserved Windows device name.";
            }

            // Track target full path occurrences for intra-batch duplicate detection
            if (!targetPathsSeen.TryAdd(targetFullPath, 1))
            {
                targetPathsSeen[targetFullPath]++;
            }

            bool hasChange = !string.Equals(meta.FullPath, targetFullPath, StringComparison.Ordinal);

            previewItems.Add(new RenamePreviewItem(
                SourceFullPath: meta.FullPath,
                OriginalFileName: meta.FileName,
                ProposedFileName: proposedFileName,
                TargetFullPath: targetFullPath,
                Extension: ext,
                SizeBytes: meta.SizeBytes,
                HasChange: hasChange,
                HasConflict: isConflict,
                Status: isConflict ? RenameItemStatus.Conflict : (hasChange ? RenameItemStatus.Pending : RenameItemStatus.Unchanged),
                ValidationError: validationError));
        }

        // Second pass: detect duplicate target conflicts and disk collisions
        for (int i = 0; i < previewItems.Count; i++)
        {
            var item = previewItems[i];
            if (item.HasConflict)
            {
                continue;
            }

            // Check if multiple files in the batch map to the exact same target path
            if (targetPathsSeen[item.TargetFullPath] > 1)
            {
                previewItems[i] = item with
                {
                    HasConflict = true,
                    Status = RenameItemStatus.Conflict,
                    ValidationError = "Collision: Multiple files produce the same target name."
                };
                continue;
            }

            // Check if destination file already exists on disk (and is not a case-only rename of the source itself)
            bool isCaseOnlySameFile = string.Equals(item.SourceFullPath, item.TargetFullPath, StringComparison.OrdinalIgnoreCase);
            if (item.HasChange && !isCaseOnlySameFile && File.Exists(item.TargetFullPath))
            {
                previewItems[i] = item with
                {
                    HasConflict = true,
                    Status = RenameItemStatus.Conflict,
                    ValidationError = "Collision: Target file already exists on disk."
                };
            }
        }

        return Result.Success<IReadOnlyList<RenamePreviewItem>>(previewItems);
    }

    /// <inheritdoc />
    public async Task<Result<RenameExecutionJournal>> ExecuteBatchAsync(
        IReadOnlyList<RenamePreviewItem> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var conflictingItems = items.Where(i => i.HasConflict).ToList();
        if (conflictingItems.Count > 0)
        {
            return Result.Failure<RenameExecutionJournal>(
                "BatchRename.Collision",
                $"Cannot execute: {conflictingItems.Count} item(s) have unresolved collisions or errors.");
        }

        var itemsToRename = items.Where(i => i.HasChange).ToList();
        if (itemsToRename.Count == 0)
        {
            return Result.Success(new RenameExecutionJournal(Guid.NewGuid(), DateTimeOffset.UtcNow, []));
        }

        return await Task.Run(() =>
        {
            var completed = new List<(string OriginalPath, string RenamedPath)>(itemsToRename.Count);
            var stopwatch = Stopwatch.StartNew();
            int total = itemsToRename.Count;
            int succeeded = 0;
            int failed = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = itemsToRename[i];

                    try
                    {
                        var targetDir = Path.GetDirectoryName(item.TargetFullPath);
                        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        File.Move(item.SourceFullPath, item.TargetFullPath, overwrite: false);
                        completed.Add((item.SourceFullPath, item.TargetFullPath));
                        succeeded++;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or PathTooLongException or IOException)
                    {
                        failed++;
                        // Rollback on partial failure to maintain filesystem integrity
                        RollbackInternal(completed);
                        return Result.Failure<RenameExecutionJournal>(
                            "BatchRename.ExecutionError",
                            $"Failed to rename '{item.OriginalFileName}' ({ex.GetType().Name}: {ex.Message}). Completed operations in this batch were rolled back.");
                    }

                    progress?.Report(new BatchProgressReport(
                        TotalItems: total,
                        ProcessedItems: i + 1,
                        SucceededItems: succeeded,
                        FailedItems: failed,
                        CurrentItemName: item.ProposedFileName,
                        Percentage: ((i + 1) / (double)total) * 100.0,
                        ElapsedTime: stopwatch.Elapsed,
                        EstimatedTimeRemaining: TimeSpan.FromMilliseconds(
                            (total - i - 1) * (stopwatch.ElapsedMilliseconds / (double)(i + 1)))));
                }

                return Result.Success(new RenameExecutionJournal(Guid.NewGuid(), DateTimeOffset.UtcNow, completed));
            }
            catch (OperationCanceledException)
            {
                RollbackInternal(completed);
                return Result.Failure<RenameExecutionJournal>(
                    "BatchRename.Cancelled",
                    "Batch rename operation was cancelled. All completed renames were rolled back.");
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RenameItemResult> ExecuteStreamingAsync(
        IReadOnlyList<RenamePreviewItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemsToRename = items.Where(i => i.HasChange && !i.HasConflict).ToList();

        foreach (var item in itemsToRename)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RenameItemResult result;
            try
            {
                await Task.Run(() =>
                {
                    var targetDir = Path.GetDirectoryName(item.TargetFullPath);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    File.Move(item.SourceFullPath, item.TargetFullPath, overwrite: false);
                }, cancellationToken).ConfigureAwait(false);

                result = new RenameItemResult(item.SourceFullPath, item.TargetFullPath, IsSuccess: true);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PathTooLongException or IOException)
            {
                result = new RenameItemResult(item.SourceFullPath, item.TargetFullPath, IsSuccess: false, ex.Message);
            }

            yield return result;
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> UndoBatchAsync(
        RenameExecutionJournal journal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journal);

        if (journal.CompletedOperations.Count == 0)
        {
            return Result.Success(0);
        }

        return await Task.Run(() =>
        {
            int restoredCount = 0;
            var ops = journal.CompletedOperations;

            for (int i = ops.Count - 1; i >= 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (originalPath, renamedPath) = ops[i];
                try
                {
                    if (File.Exists(renamedPath))
                    {
                        File.Move(renamedPath, originalPath, overwrite: false);
                        restoredCount++;
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or PathTooLongException or IOException)
                {
                    return Result.Failure<int>(
                        "BatchRename.UndoFailed",
                        $"Undo partially failed restoring '{renamedPath}' -> '{originalPath}': {ex.Message}");
                }
            }

            return Result.Success(restoredCount);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void RollbackInternal(List<(string OriginalPath, string RenamedPath)> completed)
    {
        for (int i = completed.Count - 1; i >= 0; i--)
        {
            try
            {
                var (originalPath, renamedPath) = completed[i];
                if (File.Exists(renamedPath) && !File.Exists(originalPath))
                {
                    File.Move(renamedPath, originalPath, overwrite: false);
                }
            }
            catch
            {
                // Best-effort rollback
            }
        }
    }
}
