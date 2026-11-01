using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.FolderCreator;

namespace OmniForge.App.Modules.BatchFolder;

/// <summary>
/// Service abstraction for batch folder creation and dry-run preview operations.
/// </summary>
public interface IBatchFolderService
{
    /// <summary>
    /// Executes a dry-run inspection to preview the directory tree and detect collisions.
    /// </summary>
    Result<FolderDryRunResult> InspectDryRun(
        string rootDirectory,
        string rawInput,
        FolderInputMode mode = FolderInputMode.TreeHierarchy,
        FolderConflictResolution conflictResolution = FolderConflictResolution.SkipExisting,
        int startSequence = 1,
        DateTime? referenceTime = null);

    /// <summary>
    /// Asynchronously creates directories on disk with error isolation.
    /// </summary>
    Task<Result<FolderExecutionSummary>> ExecuteCreationAsync(
        IReadOnlyList<FolderItemNode> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IBatchFolderService"/> delegating to <see cref="FolderCreatorEngine"/>.
/// </summary>
public sealed class BatchFolderService : IBatchFolderService
{
    private readonly IFolderCreatorEngine _engine;

    public BatchFolderService(IFolderCreatorEngine? engine = null)
    {
        _engine = engine ?? new FolderCreatorEngine();
    }

    /// <inheritdoc />
    public Result<FolderDryRunResult> InspectDryRun(
        string rootDirectory,
        string rawInput,
        FolderInputMode mode = FolderInputMode.TreeHierarchy,
        FolderConflictResolution conflictResolution = FolderConflictResolution.SkipExisting,
        int startSequence = 1,
        DateTime? referenceTime = null)
    {
        return _engine.InspectDryRun(rootDirectory, rawInput, mode, conflictResolution, startSequence, referenceTime);
    }

    /// <inheritdoc />
    public Task<Result<FolderExecutionSummary>> ExecuteCreationAsync(
        IReadOnlyList<FolderItemNode> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return _engine.ExecuteCreationAsync(items, progress, cancellationToken);
    }
}

