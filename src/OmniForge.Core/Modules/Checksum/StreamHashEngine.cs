using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using OmniForge.Core.Common;

namespace OmniForge.Core.Modules.Checksum;

/// <summary>
/// High-performance, zero-allocation asynchronous streaming hash engine.
/// Utilizes IncrementalHash and ArrayPool&lt;byte&gt;.Shared to avoid LOH allocations.
/// </summary>
public static class StreamHashEngine
{
    private const int DefaultBufferSize = 64 * 1024; // 64 KB sequential read buffer

    public static HashAlgorithmName ToHashAlgorithmName(SupportedHashAlgorithm algorithm) => algorithm switch
    {
        SupportedHashAlgorithm.Md5 => HashAlgorithmName.MD5,
        SupportedHashAlgorithm.Sha1 => HashAlgorithmName.SHA1,
        SupportedHashAlgorithm.Sha256 => HashAlgorithmName.SHA256,
        SupportedHashAlgorithm.Sha384 => HashAlgorithmName.SHA384,
        SupportedHashAlgorithm.Sha512 => HashAlgorithmName.SHA512,
        _ => HashAlgorithmName.SHA256
    };

    /// <summary>
    /// Computes hash for an open stream asynchronously using rented buffers.
    /// </summary>
    public static async Task<Result<FileHashResult>> ComputeStreamHashAsync(
        Stream stream,
        SupportedHashAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var hashAlgorithm = ToHashAlgorithmName(algorithm);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        var sw = Stopwatch.StartNew();

        try
        {
            long totalLength = 0;
            try
            {
                if (stream.CanSeek)
                {
                    totalLength = stream.Length;
                }
            }
            catch
            {
                // Stream might not support seeking or length
            }

            long totalRead = 0;
            using var hasher = IncrementalHash.CreateHash(hashAlgorithm);
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, DefaultBufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                hasher.AppendData(buffer.AsSpan(0, bytesRead));
                totalRead += bytesRead;

                if (progress is not null && totalLength > 0)
                {
                    progress.Report((double)totalRead / totalLength * 100.0);
                }
            }

            Span<byte> hashBytes = stackalloc byte[hasher.HashLengthInBytes];
            hasher.GetHashAndReset(hashBytes);
            string hex = Convert.ToHexStringLower(hashBytes);

            return Result.Success(new FileHashResult(hashAlgorithm.Name ?? "SHA256", hex, totalRead, sw.Elapsed));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<FileHashResult>(Error.Cancelled);
        }
        catch (Exception ex)
        {
            return Result.Failure<FileHashResult>("Hash.StreamError", ex.Message);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Computes file checksum asynchronously from the filesystem.
    /// </summary>
    public static async Task<Result<FileHashResult>> ComputeFileHashAsync(
        string filePath,
        SupportedHashAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Result.Failure<FileHashResult>("Hash.InvalidPath", "File path cannot be empty.");

        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                BufferSize = DefaultBufferSize
            };

            await using var stream = new FileStream(filePath, options);
            return await ComputeStreamHashAsync(stream, algorithm, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<FileHashResult>(Error.Cancelled);
        }
        catch (FileNotFoundException ex)
        {
            return Result.Failure<FileHashResult>("IO.FileNotFound", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure<FileHashResult>("IO.AccessDenied", ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Failure<FileHashResult>("IO.Error", ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Failure<FileHashResult>("Hash.Error", ex.Message);
        }
    }

    /// <summary>
    /// Verifies if a computed hash matches an expected hash string.
    /// </summary>
    public static ChecksumComparisonResult CompareHashes(string computedHash, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(computedHash))
            return new ChecksumComparisonResult(false, computedHash, expectedHash, "Computed hash is empty.");

        if (string.IsNullOrWhiteSpace(expectedHash))
            return new ChecksumComparisonResult(false, computedHash, expectedHash, "Expected hash is empty.");

        string normComputed = computedHash.Trim();
        string normExpected = expectedHash.Trim();

        bool isMatch = string.Equals(normComputed, normExpected, StringComparison.OrdinalIgnoreCase);

        return new ChecksumComparisonResult(
            isMatch,
            normComputed,
            normExpected,
            isMatch ? "Checksum matches perfectly!" : "Checksum mismatch. The digests do not match.");
    }
}

