using CommunityToolkit.Mvvm.ComponentModel;
using OmniForge.Core.Common;

namespace OmniForge.Core.Models;

/// <summary>
/// Token-efficient, fault-isolated base class for all OmniForge feature ViewModels.
/// Centralizes busy state, error message notifications, and safe async Result execution.
/// </summary>
public abstract partial class ToolViewModelBase : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Clears any active error banner message.
    /// </summary>
    protected void ClearError() => ErrorMessage = null;

    /// <summary>
    /// Executes an asynchronous operation returning a <see cref="Result{T}"/> with automatic
    /// <see cref="IsBusy"/> toggling, cancellation handling, and <see cref="ErrorMessage"/> population.
    /// </summary>
    protected async Task<T?> RunSafeAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        string? busyStatus = null,
        string? successStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (!string.IsNullOrWhiteSpace(busyStatus))
            {
                StatusMessage = busyStatus;
            }

            var result = await operation(cancellationToken);
            if (result.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(successStatus))
                {
                    StatusMessage = successStatus;
                }
                return result.Value;
            }

            ErrorMessage = result.Error.Message;
            StatusMessage = "Operation failed.";
            return default;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation was cancelled.";
            return default;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
            StatusMessage = "Operation failed.";
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Executes an asynchronous operation returning a non-generic <see cref="Result"/> with automatic
    /// <see cref="IsBusy"/> toggling, cancellation handling, and <see cref="ErrorMessage"/> population.
    /// </summary>
    protected async Task<bool> RunSafeAsync(
        Func<CancellationToken, Task<Result>> operation,
        string? busyStatus = null,
        string? successStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (!string.IsNullOrWhiteSpace(busyStatus))
            {
                StatusMessage = busyStatus;
            }

            var result = await operation(cancellationToken);
            if (result.IsSuccess)
            {
                if (!string.IsNullOrWhiteSpace(successStatus))
                {
                    StatusMessage = successStatus;
                }
                return true;
            }

            ErrorMessage = result.Error.Message;
            StatusMessage = "Operation failed.";
            return false;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation was cancelled.";
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
            StatusMessage = "Operation failed.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

