using System.Diagnostics;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Core.Modules.FolderCreator;

/// <summary>
/// Core engine implementing dry-run inspection, path validation, conflict resolution,
/// and asynchronous fault-isolated folder creation.
/// </summary>
public sealed class FolderCreatorEngine : IFolderCreatorEngine
{
    /// <inheritdoc />
    public Result<FolderDryRunResult> InspectDryRun(
        string rootDirectory,
        string rawInput,
        FolderInputMode mode = FolderInputMode.TreeHierarchy,
        FolderConflictResolution conflictResolution = FolderConflictResolution.SkipExisting,
        int startSequence = 1,
        DateTime? referenceTime = null)
    {
        var rootValidation = SecurityGuard.ValidateSafePath(rootDirectory);
        if (rootValidation.IsFailure)
        {
            return Result.Failure<FolderDryRunResult>(rootValidation.Error.Code, rootValidation.Error.Message);
        }

        string validatedRoot = rootValidation.Value;

        var parseResult = FolderParser.Parse(rawInput, mode, startSequence, referenceTime);
        if (parseResult.IsFailure)
        {
            return Result.Failure<FolderDryRunResult>(parseResult.Error.Code, parseResult.Error.Message);
        }

        var rootNodes = parseResult.Value;
        var allItems = new List<FolderItemNode>();
        var validationErrors = new List<string>();

        int newCount = 0;
        int existingCount = 0;
        int suffixAppendedCount = 0;
        int invalidCount = 0;

        // Traverse tree in pre-order to calculate paths and resolve conflicts
        foreach (var rootNode in rootNodes)
        {
            ProcessNodeRecursive(rootNode, null);
        }

        void ProcessNodeRecursive(FolderItemNode node, FolderItemNode? parent)
        {
            allItems.Add(node);

            // Compute relative path
            node.RelativePath = parent is not null
                ? Path.Combine(parent.RelativePath, node.EvaluatedName)
                : node.EvaluatedName;

            // Compute full path candidate
            string candidateFullPath = Path.Combine(validatedRoot, node.RelativePath);

            // Validate against path traversal
            var pathCheck = SecurityGuard.ValidateSafePath(candidateFullPath, validatedRoot);
            if (pathCheck.IsFailure)
            {
                node.Status = FolderItemStatus.Invalid;
                node.ErrorMessage = pathCheck.Error.Message;
                node.FullPath = candidateFullPath;
                invalidCount++;
                validationErrors.Add($"Line {node.LineNumber}: {pathCheck.Error.Message}");
            }
            else
            {
                node.FullPath = pathCheck.Value;
                bool exists = Directory.Exists(node.FullPath);

                if (!exists)
                {
                    node.Status = FolderItemStatus.PlannedNew;
                    newCount++;
                }
                else
                {
                    existingCount++;
                    switch (conflictResolution)
                    {
                        case FolderConflictResolution.SkipExisting:
                            node.Status = FolderItemStatus.SkippedExisting;
                            break;

                        case FolderConflictResolution.Overwrite:
                            node.Status = FolderItemStatus.WillOverwrite;
                            break;

                        case FolderConflictResolution.AppendSuffix:
                            ResolveAppendSuffix(node, parent, validatedRoot);
                            suffixAppendedCount++;
                            break;
                    }
                }
            }

            // Recurse children
            foreach (var child in node.Children)
            {
                ProcessNodeRecursive(child, node);
            }
        }

        return Result.Success(new FolderDryRunResult(
            RootNodes: rootNodes,
            AllItems: allItems,
            TotalCount: allItems.Count,
            NewCount: newCount,
            ExistingCount: existingCount,
            SuffixAppendedCount: suffixAppendedCount,
            InvalidCount: invalidCount,
            ValidationErrors: validationErrors));
    }

    private static void ResolveAppendSuffix(FolderItemNode node, FolderItemNode? parent, string validatedRoot)
    {
        string baseName = node.EvaluatedName;
        int suffix = 1;
        string resolvedName;
        string resolvedFullPath;

        do
        {
            resolvedName = $"{baseName} ({suffix})";
            string relativeCandidate = parent is not null
                ? Path.Combine(parent.RelativePath, resolvedName)
                : resolvedName;
            resolvedFullPath = Path.Combine(validatedRoot, relativeCandidate);
            suffix++;
        } while (Directory.Exists(resolvedFullPath));

        node.EvaluatedName = resolvedName;
        node.RelativePath = parent is not null
            ? Path.Combine(parent.RelativePath, resolvedName)
            : resolvedName;
        node.FullPath = resolvedFullPath;
        node.Status = FolderItemStatus.SuffixAppended;
    }

    /// <inheritdoc />
    public async Task<Result<FolderExecutionSummary>> ExecuteCreationAsync(
        IReadOnlyList<FolderItemNode> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        long startTimestamp = Stopwatch.GetTimestamp();

        return await Task.Run(() =>
        {
            int total = items.Count;
            int processed = 0;
            int created = 0;
            int skipped = 0;
            int failed = 0;
            var errors = new List<string>();

            void ReportCurrent(string currentItemName)
            {
                if (progress is null) return;
                double percent = total > 0 ? (double)processed / total * 100.0 : 100.0;
                var currentElapsed = Stopwatch.GetElapsedTime(startTimestamp);
                progress.Report(new BatchProgressReport(
                    TotalItems: total,
                    ProcessedItems: processed,
                    SucceededItems: created + skipped,
                    FailedItems: failed,
                    CurrentItemName: currentItemName,
                    Percentage: percent,
                    ElapsedTime: currentElapsed,
                    EstimatedTimeRemaining: TimeSpan.Zero));
            }

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.Status == FolderItemStatus.SkippedExisting)
                {
                    skipped++;
                    processed++;
                    ReportCurrent($"Skipped: {item.RelativePath}");
                    continue;
                }

                if (item.Status == FolderItemStatus.Invalid)
                {
                    failed++;
                    processed++;
                    string errMsg = item.ErrorMessage ?? "Invalid path";
                    errors.Add($"{item.RelativePath}: {errMsg}");
                    ReportCurrent($"Invalid: {item.RelativePath}");
                    continue;
                }

                try
                {
                    Directory.CreateDirectory(item.FullPath);
                    item.Status = FolderItemStatus.Success;
                    created++;
                }
                catch (Exception ex)
                {
                    // Fault isolation: error does not crash or abort remaining folders
                    item.Status = FolderItemStatus.Failed;
                    item.ErrorMessage = ex.Message;
                    failed++;
                    errors.Add($"{item.RelativePath}: {ex.Message}");
                }

                processed++;
                ReportCurrent($"Created: {item.RelativePath}");
            }

            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            return Result.Success(new FolderExecutionSummary(
                TotalCount: total,
                CreatedCount: created,
                SkippedCount: skipped,
                FailedCount: failed,
                Errors: errors,
                Elapsed: elapsed));
        }, cancellationToken).ConfigureAwait(false);
    }
}
