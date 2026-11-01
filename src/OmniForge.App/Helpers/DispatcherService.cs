using Microsoft.UI.Dispatching;
using OmniForge.Core.Contracts;

namespace OmniForge.App.Helpers;

/// <summary>
/// WinUI 3 implementation of <see cref="IDispatcherService"/> wrapping <see cref="DispatcherQueue"/>.
/// </summary>
public sealed class DispatcherService : IDispatcherService
{
    private DispatcherQueue? _dispatcherQueue;

    public DispatcherService()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public void Initialize(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
    }

    public bool HasThreadAccess => _dispatcherQueue?.HasThreadAccess ?? true;

    public bool TryEnqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
        {
            action();
            return true;
        }

        return _dispatcherQueue.TryEnqueue(() => action());
    }

    public Task EnqueueAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action().ConfigureAwait(false);
                tcs.TrySetResult();
            }
            catch (OperationCanceledException ex)
            {
                tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        if (!enqueued)
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue operation onto the WinUI DispatcherQueue."));
        }

        return tcs.Task;
    }
}

