using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Modules.BatchOps;

namespace OmniForge.App.Modules.BatchRename;

/// <summary>
/// Service abstraction for batch rename operations inside the Presentation layer.
/// </summary>
public interface IBatchRenameService
{
    /// <summary>
    /// Computes preview items by chaining rules and detecting collisions.
    /// </summary>
    Result<IReadOnlyList<RenamePreviewItem>> BuildPreview(
        IReadOnlyList<FileMetadata> files,
        IReadOnlyList<IRenameRule> rules);

    /// <summary>
    /// Executes the batch rename asynchronously offloaded to a worker thread.
    /// </summary>
    Task<Result<RenameExecutionJournal>> ExecuteBatchAsync(
        IReadOnlyList<RenamePreviewItem> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts a batch rename snapshot in reverse order.
    /// </summary>
    Task<Result<int>> UndoBatchAsync(
        RenameExecutionJournal journal,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Concrete implementation of <see cref="IBatchRenameService"/> wrapping <see cref="BatchRenameEngine"/>.
/// Automatically discovered and registered by ModuleAutoDiscoveryExtensions.
/// </summary>
public sealed class BatchRenameService : IBatchRenameService
{
    private readonly IBatchRenameEngine _engine;

    public BatchRenameService(IBatchRenameEngine? engine = null)
    {
        _engine = engine ?? new BatchRenameEngine();
    }

    public Result<IReadOnlyList<RenamePreviewItem>> BuildPreview(
        IReadOnlyList<FileMetadata> files,
        IReadOnlyList<IRenameRule> rules) => _engine.BuildPreview(files, rules);

    public Task<Result<RenameExecutionJournal>> ExecuteBatchAsync(
        IReadOnlyList<RenamePreviewItem> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default) => _engine.ExecuteBatchAsync(items, progress, cancellationToken);

    public Task<Result<int>> UndoBatchAsync(
        RenameExecutionJournal journal,
        CancellationToken cancellationToken = default) => _engine.UndoBatchAsync(journal, cancellationToken);
}

