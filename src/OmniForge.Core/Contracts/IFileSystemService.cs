using OmniForge.Core.Common;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Asynchronous, non-blocking file system abstraction service returning functional <see cref="Result"/> payloads.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Asynchronously reads all text content from the specified file wrapped in a functional <see cref="Result{T}"/>.
    /// </summary>
    Task<Result<string>> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously writes text content to the specified file, creating directories if required.
    /// </summary>
    Task<Result> WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a file exists.
    /// </summary>
    Task<Result<bool>> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a directory exists.
    /// </summary>
    Task<Result<bool>> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates files matching a search pattern asynchronously while safely skipping inaccessible paths.
    /// </summary>
    IAsyncEnumerable<string> EnumerateFilesAsync(
        string directoryPath,
        string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously calculates total size in bytes of a directory wrapped in a functional <see cref="Result{T}"/>.
    /// </summary>
    Task<Result<long>> GetDirectorySizeAsync(string directoryPath, CancellationToken cancellationToken = default);
}

