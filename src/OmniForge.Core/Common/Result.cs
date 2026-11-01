using System.Diagnostics.CodeAnalysis;

namespace OmniForge.Core.Common;

/// <summary>
/// Encapsulates an error code and diagnostic error message.
/// </summary>
public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
    public static readonly Error Cancelled = new("Operation.Cancelled", "The operation was cancelled.");

    public bool HasError => !string.IsNullOrEmpty(Code);

    public override string ToString() => string.IsNullOrEmpty(Code) ? "None" : $"[{Code}] {Message}";
}

/// <summary>
/// Represents the outcome of an operation without a return value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error.HasError)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && !error.HasError)
        {
            throw new InvalidOperationException("A failure result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    public static Result Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Result(false, new Error(exception.GetType().Name, exception.Message));
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(new Error(code, message));

    public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);
}

/// <summary>
/// Represents the outcome of an operation producing a typed value of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the returned value.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the payload value when the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to access value on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access {nameof(Value)} on a failed result: {Error}");

    /// <summary>
    /// Attempts to extract the value safely.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        if (IsSuccess && _value is not null)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(true, value, Error.None);
    }

    public new static Result<T> Failure(Error error) => new(false, default, error);

    public new static Result<T> Failure(string code, string message) => new(false, default, new Error(code, message));

    public new static Result<T> Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Result<T>(false, default, new Error(exception.GetType().Name, exception.Message));
    }

    /// <summary>
    /// Maps the result payload to another type via <paramref name="selector"/> if successful.
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return IsSuccess ? Result<TResult>.Success(selector(Value)) : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Chains another operation returning a <see cref="Result{TResult}"/> if this result is successful.
    /// </summary>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return IsSuccess ? binder(Value) : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Matches on the success or failure states.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

