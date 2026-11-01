using OmniForge.Core.Common;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.FolderCreator;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Engine contract for dry-run inspection and asynchronous creation of single or nested directory structures.
/// </summary>
public interface IFolderCreatorEngine
{
    /// <summary>
    /// Performs a non-destructive dry-run inspection returning directory tree preview with collision diagnostics.
    /// </summary>
    Result<FolderDryRunResult> InspectDryRun(
        string rootDirectory,
        string rawInput,
        FolderInputMode mode = FolderInputMode.TreeHierarchy,
        FolderConflictResolution conflictResolution = FolderConflictResolution.SkipExisting,
        int startSequence = 1,
        DateTime? referenceTime = null);

    /// <summary>
    /// Asynchronously creates planned directories on disk with error isolation and progress reporting.
    /// </summary>
    Task<Result<FolderExecutionSummary>> ExecuteCreationAsync(
        IReadOnlyList<FolderItemNode> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

