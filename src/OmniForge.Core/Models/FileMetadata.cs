namespace OmniForge.Core.Models;

/// <summary>
/// Encapsulates rich file metadata for batch operations, token interpolation, and validation.
/// </summary>
public sealed record FileMetadata(
    string FullPath,
    string DirectoryName,
    string FileName,
    string Extension,
    long SizeBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt)
{
    /// <summary>
    /// Creates a <see cref="FileMetadata"/> instance from a physical file path.
    /// Safely handles non-existent or inaccessible files.
    /// </summary>
    public static FileMetadata FromPath(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        try
        {
            var fileInfo = new FileInfo(filePath);
            return new FileMetadata(
                FullPath: fileInfo.FullName,
                DirectoryName: fileInfo.DirectoryName ?? string.Empty,
                FileName: fileInfo.Name,
                Extension: fileInfo.Extension,
                SizeBytes: fileInfo.Exists ? fileInfo.Length : 0L,
                CreatedAt: fileInfo.Exists ? fileInfo.CreationTimeUtc : DateTimeOffset.UtcNow,
                ModifiedAt: fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTimeOffset.UtcNow);
        }
        catch
        {
            return new FileMetadata(
                FullPath: filePath,
                DirectoryName: Path.GetDirectoryName(filePath) ?? string.Empty,
                FileName: Path.GetFileName(filePath),
                Extension: Path.GetExtension(filePath),
                SizeBytes: 0L,
                CreatedAt: DateTimeOffset.UtcNow,
                ModifiedAt: DateTimeOffset.UtcNow);
        }
    }
}

