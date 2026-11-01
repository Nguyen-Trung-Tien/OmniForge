---
name: batch-io-processor
description: Provides guidelines, architecture patterns, and production-ready code samples for chunked, cancellable batch file processing in OmniForge.
---

# Batch I/O Processor Skill

This skill provides guidelines and robust architectural patterns for processing large volumes of files or gigabyte-scale streams without blocking the UI thread or exhausting system memory.

---

## 1. Core Principles of Batch Processing

1. **Throttled Concurrency**: Unbounded parallel file access causes I/O thrashing, disk queue bottlenecks, and OS handle exhaustion. Throttle concurrent operations using `SemaphoreSlim` or `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` (typically `Environment.ProcessorCount` for CPU-bound tasks, or 4-8 for disk I/O).
2. **Channel-Based Producer-Consumer**: Decouple file discovery (directory crawling) from file content processing using `System.Threading.Channels.Channel<T>`.
3. **Structured Progress Reporting**: Utilize `IProgress<BatchProgress>` to report processed items, failure counts, and estimated time remaining back to the UI thread via `DispatcherQueue`.
4. **Resilience & Fault Isolation**: An error on a single file (e.g. `UnauthorizedAccessException`, locked file) must be recorded as a file error and must not terminate the entire batch.

---

## 2. Production Code Example: Batch Processor

```csharp
using System.Buffers;
using System.Threading.Channels;
using OmniForge.Core.Common;

namespace OmniForge.Services.Batch;

public sealed record BatchItem(string FilePath, long FileSizeBytes);

public sealed record BatchProgress(
    int TotalItems, 
    int ProcessedItems, 
    int FailedItems, 
    string CurrentFile, 
    double PercentComplete);

public interface IBatchProcessor
{
    Task<Result<BatchSummary>> ProcessFilesAsync(
        IReadOnlyList<string> filePaths,
        Func<string, CancellationToken, Task> fileAction,
        IProgress<BatchProgress>? progress = null,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default);
}

public sealed record BatchSummary(int Total, int Succeeded, int Failed, TimeSpan Duration);

public sealed class BatchProcessor : IBatchProcessor
{
    public async Task<Result<BatchSummary>> ProcessFilesAsync(
        IReadOnlyList<string> filePaths,
        Func<string, CancellationToken, Task> fileAction,
        IProgress<BatchProgress>? progress = null,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentNullException.ThrowIfNull(fileAction);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int total = filePaths.Count;
        int processed = 0;
        int failed = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism),
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(filePaths, parallelOptions, async (filePath, token) =>
            {
                try
                {
                    await fileAction(filePath, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failed);
                }
                finally
                {
                    int currentProcessed = Interlocked.Increment(ref processed);
                    if (progress is not null)
                    {
                        double percent = total > 0 ? (double)currentProcessed / total * 100.0 : 100.0;
                        progress.Report(new BatchProgress(total, currentProcessed, failed, filePath, percent));
                    }
                }
            }).ConfigureAwait(false);

            stopwatch.Stop();
            return Result<BatchSummary>.Success(new BatchSummary(total, processed - failed, failed, stopwatch.Elapsed));
        }
        catch (OperationCanceledException)
        {
            return Result<BatchSummary>.Failure(new Error("Batch.Cancelled", "The batch operation was cancelled by the user."));
        }
        catch (Exception ex)
        {
            return Result<BatchSummary>.Failure(new Error("Batch.Failed", $"Critical batch execution error: {ex.Message}"));
        }
    }
}
```

---

## 3. High-Throughput Chunked Stream Processing

For hashing, encrypting, or analyzing massive single files:

```csharp
public static async Task<Result<long>> ProcessFileInChunksAsync(
    string filePath, 
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> chunkHandler,
    int chunkSize = 64 * 1024, // 64 KB default chunk
    CancellationToken cancellationToken = default)
{
    byte[] rentBuffer = ArrayPool<byte>.Shared.Rent(chunkSize);
    long totalBytesRead = 0;

    try
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            BufferSize = chunkSize
        };

        await using var stream = new FileStream(filePath, options);
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(rentBuffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalBytesRead += bytesRead;

            await chunkHandler(rentBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }

        return Result<long>.Success(totalBytesRead);
    }
    catch (Exception ex)
    {
        return Result<long>.Failure(new Error("ChunkProcessing.Failed", ex.Message));
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(rentBuffer);
    }
}
```

