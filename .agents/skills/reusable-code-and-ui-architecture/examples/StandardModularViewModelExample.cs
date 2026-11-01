using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Example;

public interface IStandardProcessorService
{
    Task<Result<string>> ProcessAsync(string input, CancellationToken cancellationToken = default);
}

public sealed partial class StandardModularViewModelExample(IStandardProcessorService processorService) : ToolViewModelBase
{
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultText { get; set; } = string.Empty;

    public bool IsEmpty => string.IsNullOrEmpty(ResultText);
    public bool HasResults => !IsEmpty;

    private bool CanProcess => !string.IsNullOrWhiteSpace(InputText) && IsNotBusy;

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var output = await RunSafeAsync(
            ct => processorService.ProcessAsync(InputText, ct),
            busyStatus: "Executing standard pipeline...",
            successStatus: "Operation completed.",
            cancellationToken: cancellationToken);

        if (output is not null)
        {
            ResultText = output;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasResults));
        }
    }
}

