using OmniForge.Core.Common;

namespace OmniForge.Modules.BatchOps;

/// <summary>
/// Status state for an individual file preview or execution item.
/// </summary>
public enum RenameItemStatus
{
    Unchanged,
    Pending,
    Conflict,
    Success,
    Error,
    Skipped
}

/// <summary>
/// Immutable preview item representing a planned rename operation with collision diagnostic information.
/// </summary>
public sealed record RenamePreviewItem(
    string SourceFullPath,
    string OriginalFileName,
    string ProposedFileName,
    string TargetFullPath,
    string Extension,
    long SizeBytes,
    bool HasChange,
    bool HasConflict,
    RenameItemStatus Status,
    string? ValidationError = null)
{
    /// <summary>
    /// Culture-invariant human-readable formatted file size (e.g. "1.5 MB").
    /// </summary>
    public string FormattedSize => ByteSizeFormatter.FormatBytes(SizeBytes);

    /// <summary>
    /// True when the file name is unchanged and has no validation conflicts.
    /// </summary>
    public bool IsUnchanged => !HasChange && !HasConflict;

    /// <summary>
    /// True when the item has a diagnostic validation or collision error message.
    /// </summary>
    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);

    /// <summary>
    /// Parent directory path for quick context inspection.
    /// </summary>
    public string DirectoryPath => Path.GetDirectoryName(SourceFullPath) ?? string.Empty;
}

/// <summary>
/// Transactional journal snapshot capturing executed rename operations for reversible rollback.
/// </summary>
public sealed record RenameExecutionJournal(
    Guid BatchId,
    DateTimeOffset ExecutedAtUtc,
    IReadOnlyList<(string OriginalPath, string RenamedPath)> CompletedOperations)
{
    /// <summary>
    /// Empty journal representing zero operations.
    /// </summary>
    public static RenameExecutionJournal Empty => new(Guid.Empty, DateTimeOffset.MinValue, []);
}

/// <summary>
/// Streamed progress outcome for a single file operation.
/// </summary>
public sealed record RenameItemResult(
    string OriginalPath,
    string RenamedPath,
    bool IsSuccess,
    string? ErrorMessage = null);

