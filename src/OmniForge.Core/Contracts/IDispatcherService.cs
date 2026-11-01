namespace OmniForge.Core.Contracts;

/// <summary>
/// Abstraction over the UI thread dispatcher, enabling testable ViewModel thread marshalling.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Gets a value indicating whether the current thread is bound to the UI dispatcher.
    /// </summary>
    bool HasThreadAccess { get; }

    /// <summary>
    /// Enqueues a synchronous action to be executed on the UI thread.
    /// </summary>
    bool TryEnqueue(Action action);

    /// <summary>
    /// Executes an asynchronous action on the UI thread and awaits its completion.
    /// </summary>
    Task EnqueueAsync(Func<Task> action, CancellationToken cancellationToken = default);
}

