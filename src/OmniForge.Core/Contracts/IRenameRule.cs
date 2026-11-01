using OmniForge.Core.Models;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Defines an extensible transformation rule for file names in batch rename operations.
/// </summary>
public interface IRenameRule
{
    /// <summary>
    /// User-friendly label or name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Indicates whether the rule is currently active in the transformation pipeline.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Applies the transformation rule to the input file name.
    /// </summary>
    /// <param name="originalName">The file name (stem) before this rule executes.</param>
    /// <param name="index">The zero-based index of the file in the batch.</param>
    /// <param name="metadata">Rich file metadata for token substitution and contextual evaluation.</param>
    /// <returns>The transformed file name.</returns>
    string Apply(string originalName, int index, FileMetadata metadata);
}

