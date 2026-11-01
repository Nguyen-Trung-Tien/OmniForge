namespace OmniForge.Core.Common;

/// <summary>
/// Functional pipeline extensions for <see cref="Result"/> and <see cref="Result{T}"/> to eliminate repetitive if/else branching.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Validates a predicate against a successful result value, converting to failure if false.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        if (result.IsFailure) return result;
        return predicate(result.Value) ? result : Result.Failure<T>(error);
    }

    /// <summary>
    /// Executes a side-effect action when the result is successful without altering the result.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Asynchronously maps a <see cref="Result{T}"/> task to <see cref="Result{TResult}"/>.
    /// </summary>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Task<Result<T>> resultTask,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(selector);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result.Success(selector(result.Value))
            : Result.Failure<TResult>(result.Error);
    }

    /// <summary>
    /// Asynchronously chains another <see cref="Result{TResult}"/>-returning operation when the initial result succeeds.
    /// </summary>
    public static async Task<Result<TResult>> BindAsync<T, TResult>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await binder(result.Value).ConfigureAwait(false)
            : Result.Failure<TResult>(result.Error);
    }
}

