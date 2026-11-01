namespace OmniForge.Core.Modules.FolderCreator;

/// <summary>
/// Input parsing mode for raw text folder specifications.
/// </summary>
public enum FolderInputMode
{
    /// <summary>
    /// Indentation (2 spaces, 4 spaces, or Tabs) defines the parent-child hierarchy.
    /// </summary>
    TreeHierarchy = 0,

    /// <summary>
    /// Each line is a standalone folder name or relative path.
    /// </summary>
    Flat = 1
}

/// <summary>
/// Resolution behavior when a planned directory already exists on disk.
/// </summary>
public enum FolderConflictResolution
{
    /// <summary>
    /// Skips creation if the folder already exists.
    /// </summary>
    SkipExisting = 0,

    /// <summary>
    /// Merges into or keeps the existing folder without modification.
    /// </summary>
    Overwrite = 1,

    /// <summary>
    /// Appends an incremented numeric suffix, e.g. "Folder (1)", "Folder (2)".
    /// </summary>
    AppendSuffix = 2
}

/// <summary>
/// State tracking for planned and executed folder creation operations.
/// </summary>
public enum FolderItemStatus
{
    Pending,
    PlannedNew,
    SkippedExisting,
    WillOverwrite,
    SuffixAppended,
    Success,
    Failed,
    Invalid
}

/// <summary>
/// Hierarchical tree node representing a planned folder.
/// </summary>
public sealed class FolderItemNode
{
    public string RawText { get; init; } = string.Empty;

    public string EvaluatedName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public int IndentLevel { get; init; }

    public int LineNumber { get; init; }

    public List<FolderItemNode> Children { get; } = [];

    public FolderItemNode? Parent { get; set; }

    public FolderItemStatus Status { get; set; } = FolderItemStatus.Pending;

    public string? ErrorMessage { get; set; }

    public void AddChild(FolderItemNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent = this;
        Children.Add(child);
    }

    /// <summary>
    /// Flattens this node and all descendants in pre-order traversal (parent before children).
    /// </summary>
    public IEnumerable<FolderItemNode> Flatten()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var descendant in child.Flatten())
            {
                yield return descendant;
            }
        }
    }
}

/// <summary>
/// Result of dry-run inspection containing hierarchical and flat views with collision statistics.
/// </summary>
public sealed record FolderDryRunResult(
    IReadOnlyList<FolderItemNode> RootNodes,
    IReadOnlyList<FolderItemNode> AllItems,
    int TotalCount,
    int NewCount,
    int ExistingCount,
    int SuffixAppendedCount,
    int InvalidCount,
    IReadOnlyList<string> ValidationErrors);

/// <summary>
/// Execution summary capturing creation outcomes with fault isolation logs.
/// </summary>
public sealed record FolderExecutionSummary(
    int TotalCount,
    int CreatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> Errors,
    TimeSpan Elapsed);

