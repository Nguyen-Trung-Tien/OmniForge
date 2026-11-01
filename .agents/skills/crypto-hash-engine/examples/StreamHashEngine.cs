using System.Buffers;
using System.Security.Cryptography;
using OmniForge.Core.Common;

namespace OmniForge.Services.Cryptography;

public sealed record FileHashResult(string Algorithm, string HexDigest, long BytesProcessed, TimeSpan Duration);

public static class StreamHashEngine
{
    private const int DefaultBufferSize = 64 * 1024; // 64 KB sequential read buffer

    public static async Task<Result<FileHashResult>> ComputeFileHashAsync(
        string filePath,
        HashAlgorithmName algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Result.Failure<FileHashResult>("Hash.InvalidPath", "File path cannot be empty.");

        byte[] buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        var sw = System.Diagnostics.Stopwatch.StartNew();

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
            long totalLength = stream.Length;
            long totalRead = 0;

            using var hasher = IncrementalHash.CreateHash(algorithm);
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

            return Result.Success(new FileHashResult(algorithm.Name ?? "SHA256", hex, totalRead, sw.Elapsed));
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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

