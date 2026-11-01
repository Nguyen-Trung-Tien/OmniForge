using System.Runtime.CompilerServices;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;

namespace OmniForge.Services;

/// <summary>
/// Asynchronous implementation of <see cref="IFileSystemService"/> strictly avoiding UI thread blocking
/// and wrapping expected operational exceptions in functional <see cref="Result"/> payloads.
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    public async Task<Result<string>> ReadAllTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure<string>("IO.InvalidPath", "File path cannot be empty.");
        }

        try
        {
            var normalizedPath = NormalizeExtendedPath(filePath);
            await using var stream = new FileStream(
                normalizedPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(text);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<string>(Error.Cancelled);
        }
        catch (FileNotFoundException ex)
        {
            return Result.Failure<string>("IO.FileNotFound", ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return Result.Failure<string>("IO.DirectoryNotFound", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure<string>("IO.AccessDenied", ex.Message);
        }
        catch (PathTooLongException ex)
        {
            return Result.Failure<string>("IO.PathTooLong", ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Failure<string>("IO.Error", ex.Message);
        }
    }

    public async Task<Result> WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure("IO.InvalidPath", "File path cannot be empty.");
        }

        try
        {
            var normalizedPath = NormalizeExtendedPath(filePath);
            var directory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrEmpty(directory))
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }

            await using var stream = new FileStream(
                normalizedPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(Error.Cancelled);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure("IO.AccessDenied", ex.Message);
        }
        catch (PathTooLongException ex)
        {
            return Result.Failure("IO.PathTooLong", ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Failure("IO.Error", ex.Message);
        }
    }

    public async Task<Result<bool>> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure<bool>("IO.InvalidPath", "File path cannot be empty.");
        }

        try
        {
            var normalizedPath = NormalizeExtendedPath(filePath);
            bool exists = await Task.Run(() => File.Exists(normalizedPath), cancellationToken).ConfigureAwait(false);
            return Result.Success(exists);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<bool>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>("IO.Error", ex.Message);
        }
    }

    public async Task<Result<bool>> DirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return Result.Failure<bool>("IO.InvalidPath", "Directory path cannot be empty.");
        }

        try
        {
            var normalizedPath = NormalizeExtendedPath(directoryPath);
            bool exists = await Task.Run(() => Directory.Exists(normalizedPath), cancellationToken).ConfigureAwait(false);
            return Result.Success(exists);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<bool>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>("IO.Error", ex.Message);
        }
    }

    public async IAsyncEnumerable<string> EnumerateFilesAsync(
        string directoryPath,
        string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var normalizedPath = NormalizeExtendedPath(directoryPath);
        var existsResult = await DirectoryExistsAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
        if (!existsResult.IsSuccess || !existsResult.Value)
        {
            yield break;
        }

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = searchOption == SearchOption.AllDirectories,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System
        };

        var files = await Task.Run(() => Directory.EnumerateFiles(normalizedPath, searchPattern, options), cancellationToken)
            .ConfigureAwait(false);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }
    }

    public async Task<Result<long>> GetDirectorySizeAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return Result.Failure<long>("IO.InvalidPath", "Directory path cannot be empty.");
        }

        try
        {
            var normalizedPath = NormalizeExtendedPath(directoryPath);
            long totalSize = await Task.Run(() =>
            {
                if (!Directory.Exists(normalizedPath))
                {
                    return -1L;
                }

                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System
                };

                var dirInfo = new DirectoryInfo(normalizedPath);
                long sum = 0;
                foreach (var fileInfo in dirInfo.EnumerateFiles("*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        sum += fileInfo.Length;
                    }
                    catch (IOException)
                    {
                        // Skip transient locked/deleted file during size calculation
                    }
                }

                return sum;
            }, cancellationToken).ConfigureAwait(false);

            if (totalSize < 0)
            {
                return Result.Failure<long>("IO.DirectoryNotFound", $"Directory '{directoryPath}' was not found.");
            }

            return Result.Success(totalSize);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<long>(Error.Cancelled);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure<long>("IO.AccessDenied", ex.Message);
        }
        catch (PathTooLongException ex)
        {
            return Result.Failure<long>("IO.PathTooLong", ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Failure<long>("IO.Error", ex.Message);
        }
    }

    public static string NormalizeExtendedPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (fullPath.Length >= 260 && !fullPath.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return @"\\?\" + fullPath;
        }

        return fullPath;
    }
}
