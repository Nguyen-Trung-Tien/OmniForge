using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniForge.Core.Common;

namespace OmniForge.Core.Models;

/// <summary>
/// Token-efficient, fault-isolated base class for all OmniForge feature ViewModels.
/// Centralizes busy state, progress reporting, execution timing, error notifications, and safe async Result execution.
/// </summary>
public abstract partial class ToolViewModelBase : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; } = true;

    [ObservableProperty]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready.";

    [ObservableProperty]
    public partial string LastDurationText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError
    {
        get => !string.IsNullOrEmpty(ErrorMessage);
        set
        {
            if (!value && !string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = null;
            }
        }
    }

    /// <summary>
    /// Clears any active error banner message.
    /// </summary>
    protected void ClearError() => ErrorMessage = null;

    /// <summary>
    /// Updates determinate batch progress (0..100) and status message in one call.
    /// </summary>
    protected void ReportProgress(double percent, string? status = null)
    {
        IsIndeterminate = false;
        ProgressPercent = Math.Clamp(percent, 0.0, 100.0);
        if (!string.IsNullOrWhiteSpace(status))
        {
            StatusMessage = status;
        }
    }

    /// <summary>
    /// Executes an asynchronous operation returning a <see cref="Result{T}"/> with automatic
    /// <see cref="IsBusy"/> toggling, high-resolution timing, cancellation handling, and <see cref="ErrorMessage"/> population.
    /// </summary>
    protected async Task<T?> RunSafeAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        string? busyStatus = null,
        string? successStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            IsBusy = true;
            IsIndeterminate = true;
            ErrorMessage = null;
            if (!string.IsNullOrWhiteSpace(busyStatus))
            {
                StatusMessage = busyStatus;
            }

            var result = await operation(cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);

            if (result.IsSuccess)
            {
                StatusMessage = !string.IsNullOrWhiteSpace(successStatus)
                    ? $"{successStatus} ({LastDurationText})"
                    : $"Completed in {LastDurationText}.";
                return result.Value;
            }

            ErrorMessage = result.Error.Message;
            StatusMessage = $"Operation failed ({LastDurationText}).";
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
    /// <see cref="IsBusy"/> toggling, high-resolution timing, cancellation handling, and <see cref="ErrorMessage"/> population.
    /// </summary>
    protected async Task<bool> RunSafeAsync(
        Func<CancellationToken, Task<Result>> operation,
        string? busyStatus = null,
        string? successStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            IsBusy = true;
            IsIndeterminate = true;
            ErrorMessage = null;
            if (!string.IsNullOrWhiteSpace(busyStatus))
            {
                StatusMessage = busyStatus;
            }

            var result = await operation(cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);

            if (result.IsSuccess)
            {
                StatusMessage = !string.IsNullOrWhiteSpace(successStatus)
                    ? $"{successStatus} ({LastDurationText})"
                    : $"Completed in {LastDurationText}.";
                return true;
            }

            ErrorMessage = result.Error.Message;
            StatusMessage = $"Operation failed ({LastDurationText}).";
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
