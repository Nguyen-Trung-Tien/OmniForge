using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Example;

public interface IExampleWorkerService
{
    Task<Result<string>> ProcessAsync(string input, CancellationToken cancellationToken = default);
}

public sealed partial class SafeViewModelCommandExample(IExampleWorkerService workerService) : ToolViewModelBase
{
    [ObservableProperty]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputResult { get; set; } = string.Empty;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ExecuteHeavyWorkAsync(CancellationToken cancellationToken)
    {
        // 1. Pre-flight security & input validation
        var check = SecurityGuard.ValidatePayloadSize(InputText);
        if (check.IsFailure)
        {
            ErrorMessage = check.Error.Message;
            return;
        }

        // 2. Fault-isolated execution with automatic timing, IsBusy, and exception shielding
        var output = await RunSafeAsync(
            ct => workerService.ProcessAsync(InputText, ct),
            busyStatus: "Processing payload safely...",
            successStatus: "Operation completed successfully.",
            cancellationToken: cancellationToken);

        // 3. Safe UI-thread mutation only when output succeeded
        if (output is not null)
        {
            OutputResult = output;
        }
    }
}

