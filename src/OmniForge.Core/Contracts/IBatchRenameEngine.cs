using OmniForge.Core.Common;
using OmniForge.Core.Models;
using OmniForge.Modules.BatchOps;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Defines the engine contract for validating, executing, and reverting batch rename operations.
/// </summary>
public interface IBatchRenameEngine
{
    /// <summary>
    /// Computes deterministic preview items for the supplied files by chaining the enabled rules in sequential order.
    /// Performs pre-flight collision checks for invalid characters, Windows reserved names, intra-batch duplicates, and existing disk targets.
    /// </summary>
    Result<IReadOnlyList<RenamePreviewItem>> BuildPreview(
        IReadOnlyList<FileMetadata> files,
        IReadOnlyList<IRenameRule> rules);

    /// <summary>
    /// Executes the batch rename asynchronously using worker thread offloading.
    /// Emits progress reports and returns an undo journal snapshot for transactional safety.
    /// </summary>
    Task<Result<RenameExecutionJournal>> ExecuteBatchAsync(
        IReadOnlyList<RenamePreviewItem> items,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts a previously executed batch rename by restoring files back to their original paths in reverse topological order.
    /// </summary>
    Task<Result<int>> UndoBatchAsync(
        RenameExecutionJournal journal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the batch rename as a stream of individual results via <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    IAsyncEnumerable<RenameItemResult> ExecuteStreamingAsync(
        IReadOnlyList<RenamePreviewItem> items,
        CancellationToken cancellationToken = default);
}

