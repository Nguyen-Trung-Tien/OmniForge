using System.Buffers;
using System.Diagnostics;
using OmniForge.Core.Common;
using OmniForge.Core.Models;

namespace OmniForge.Services.Batch;

/// <summary>
/// Production-grade batch processing engine utilizing throttled concurrency, 
/// buffer pooling, and non-blocking asynchronous streaming.
/// </summary>
public sealed class RobustBatchEngine
{
    private const int DefaultBufferSize = 64 * 1024; // 64 KB pool buffer

    public async Task<Result<BatchSummary>> ProcessBatchAsync(
        IReadOnlyList<string> filePaths,
        Func<string, ReadOnlyMemory<byte>, CancellationToken, Task> fileProcessor,
        IProgress<BatchProgressReport>? progress = null,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentNullException.ThrowIfNull(fileProcessor);

        var stopwatch = Stopwatch.StartNew();
        int total = filePaths.Count;
        int processed = 0;
        int succeeded = 0;
        int failed = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, maxConcurrency),
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(filePaths, parallelOptions, async (filePath, token) =>
            {
                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
                try
                {
                    var fileOptions = new FileStreamOptions
                    {
                        Mode = FileMode.Open,
                        Access = FileAccess.Read,
                        Share = FileShare.Read,
                        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                        BufferSize = DefaultBufferSize
                    };

                    await using var stream = new FileStream(filePath, fileOptions);
                    int bytesRead = await stream.ReadAsync(rentedBuffer.AsMemory(0, DefaultBufferSize), token).ConfigureAwait(false);

                    await fileProcessor(filePath, rentedBuffer.AsMemory(0, bytesRead), token).ConfigureAwait(false);
                    Interlocked.Increment(ref succeeded);
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
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                    int currentProcessed = Interlocked.Increment(ref processed);

                    if (progress is not null)
                    {
                        var elapsed = stopwatch.Elapsed;
                        double pct = total > 0 ? (double)currentProcessed / total * 100.0 : 100.0;
                        var estimatedRemaining = currentProcessed > 0
                            ? TimeSpan.FromMilliseconds((elapsed.TotalMilliseconds / currentProcessed) * (total - currentProcessed))
                            : TimeSpan.Zero;

                        progress.Report(new BatchProgressReport(
                            TotalItems: total,
                            ProcessedItems: currentProcessed,
                            SucceededItems: succeeded,
                            FailedItems: failed,
                            CurrentItemName: Path.GetFileName(filePath),
                            Percentage: pct,
                            ElapsedTime: elapsed,
                            EstimatedTimeRemaining: estimatedRemaining));
                    }
                }
            }).ConfigureAwait(false);

            stopwatch.Stop();
            return Result<BatchSummary>.Success(new BatchSummary(total, succeeded, failed, stopwatch.Elapsed));
        }
        catch (OperationCanceledException)
        {
            return Result<BatchSummary>.Failure(new Error("Batch.Cancelled", "Batch operation was cancelled by the user."));
        }
        catch (Exception ex)
        {
            return Result<BatchSummary>.Failure(new Error("Batch.FatalError", ex.Message));
        }
    }
}

