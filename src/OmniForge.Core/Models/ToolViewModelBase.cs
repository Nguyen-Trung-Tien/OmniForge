using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;

namespace OmniForge.Core.Models;

/// <summary>
/// Token-efficient, fault-isolated base class for all OmniForge feature ViewModels.
/// Centralizes busy state, progress reporting, task cancellation, confirmation banners, execution timing, error notifications, and safe async Result execution.
/// </summary>
public abstract partial class ToolViewModelBase : ObservableObject
{
    private CancellationTokenSource? _activeOperationCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyPropertyChangedFor(nameof(CanCancelOperation))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    public bool CanCancelOperation => IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeterminate))]
    public partial bool IsIndeterminate { get; set; } = true;

    public bool IsDeterminate => !IsIndeterminate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercentText))]
    public partial double ProgressPercent { get; set; }

    public string ProgressPercentText => string.Create(CultureInfo.InvariantCulture, $"{ProgressPercent:F0}%");

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccessMessage))]
    public partial string? SuccessMessage { get; set; }

    public bool HasSuccessMessage
    {
        get => !string.IsNullOrEmpty(SuccessMessage);
        set
        {
            if (!value && !string.IsNullOrEmpty(SuccessMessage))
            {
                SuccessMessage = null;
            }
        }
    }

    [ObservableProperty]
    public partial bool IsConfirmationOpen { get; set; }

    [ObservableProperty]
    public partial string ConfirmationTitle { get; set; } = "Confirm Action";

    [ObservableProperty]
    public partial string ConfirmationMessage { get; set; } = string.Empty;

    private Func<CancellationToken, Task>? _pendingConfirmedAction;

    /// <summary>
    /// Clears any active error or confirmation banners.
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Displays a non-intrusive confirmation banner before executing a destructive or large batch operation.
    /// </summary>
    protected void RequestConfirmation(string title, string message, Func<CancellationToken, Task> onConfirmedAction)
    {
        ArgumentNullException.ThrowIfNull(onConfirmedAction);
        ErrorMessage = null;
        SuccessMessage = null;
        ConfirmationTitle = title;
        ConfirmationMessage = message;
        _pendingConfirmedAction = onConfirmedAction;
        IsConfirmationOpen = true;
        StatusMessage = $"Awaiting confirmation: {title}";
    }

    [RelayCommand]
    public async Task ConfirmPendingActionAsync()
    {
        var action = _pendingConfirmedAction;
        _pendingConfirmedAction = null;
        IsConfirmationOpen = false;

        if (action is not null)
        {
            await action(CancellationToken.None);
        }
    }

    [RelayCommand]
    public void DismissConfirmation()
    {
        _pendingConfirmedAction = null;
        IsConfirmationOpen = false;
        StatusMessage = "Action cancelled by user.";
    }

    /// <summary>
    /// Cancels any currently running asynchronous task managed by this ViewModel.
    /// </summary>
    [RelayCommand]
    public void CancelCurrentOperation()
    {
        if (_activeOperationCts is { IsCancellationRequested: false })
        {
            StatusMessage = "Cancelling current operation...";
            _activeOperationCts.Cancel();
        }
    }

    /// <summary>
    /// Creates a linked <see cref="CancellationTokenSource"/> for custom long-running operations so <see cref="CancelCurrentOperation"/> can abort them.
    /// </summary>
    protected CancellationToken BeginCancellableScope(CancellationToken externalToken = default)
    {
        _activeOperationCts?.Cancel();
        _activeOperationCts?.Dispose();
        _activeOperationCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        return _activeOperationCts.Token;
    }

    /// <summary>
    /// Disposes the current cancellable operation scope.
    /// </summary>
    protected void EndCancellableScope()
    {
        _activeOperationCts?.Dispose();
        _activeOperationCts = null;
    }

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
    /// <see cref="IsBusy"/> toggling, cancellation support, high-resolution timing, and <see cref="SuccessMessage"/> / <see cref="ErrorMessage"/> population.
    /// </summary>
    protected async Task<T?> RunSafeAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        string? busyStatus = null,
        string? successStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        long startTimestamp = Stopwatch.GetTimestamp();
        var linkedToken = BeginCancellableScope(cancellationToken);

        try
        {
            IsBusy = true;
            IsIndeterminate = true;
            ProgressPercent = 0;
            ErrorMessage = null;
            SuccessMessage = null;
            IsConfirmationOpen = false;

            if (!string.IsNullOrWhiteSpace(busyStatus))
            {
                StatusMessage = busyStatus;
            }

            var result = await operation(linkedToken);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);

            if (result.IsSuccess)
            {
                string completedMsg = !string.IsNullOrWhiteSpace(successStatus)
                    ? $"{successStatus} ({LastDurationText})"
                    : $"Completed in {LastDurationText}.";

                StatusMessage = completedMsg;
                SuccessMessage = completedMsg;
                return result.Value;
            }

            ErrorMessage = result.Error.Message;
            StatusMessage = $"Operation failed ({LastDurationText}).";
            return default;
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);
            StatusMessage = $"Operation cancelled by user ({LastDurationText}).";
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
            EndCancellableScope();
            IsBusy = false;
        }
    }

    /// <summary>
    /// Executes an asynchronous operation returning a non-generic <see cref="Result"/> with automatic
    /// <see cref="IsBusy"/> toggling, cancellation support, high-resolution timing, and <see cref="SuccessMessage"/> / <see cref="ErrorMessage"/> population.
    /// </summary>
    protected async Task<bool> RunSafeAsync(
        Func<CancellationToken, Task<Result>> operation,
        string? busyStatus = null,
        string? successStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        long startTimestamp = Stopwatch.GetTimestamp();
        var linkedToken = BeginCancellableScope(cancellationToken);

        try
        {
            IsBusy = true;
            IsIndeterminate = true;
            ProgressPercent = 0;
            ErrorMessage = null;
            SuccessMessage = null;
            IsConfirmationOpen = false;

            if (!string.IsNullOrWhiteSpace(busyStatus))
            {
                StatusMessage = busyStatus;
            }

            var result = await operation(linkedToken);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);

            if (result.IsSuccess)
            {
                string completedMsg = !string.IsNullOrWhiteSpace(successStatus)
                    ? $"{successStatus} ({LastDurationText})"
                    : $"Completed in {LastDurationText}.";

                StatusMessage = completedMsg;
                SuccessMessage = completedMsg;
                return true;
            }

            ErrorMessage = result.Error.Message;
            StatusMessage = $"Operation failed ({LastDurationText}).";
            return false;
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);
            StatusMessage = $"Operation cancelled by user ({LastDurationText}).";
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
            EndCancellableScope();
            IsBusy = false;
        }
    }
}
