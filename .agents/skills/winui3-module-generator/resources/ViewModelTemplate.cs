using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.__MODULE_NAME__;

/// <summary>
/// Token-efficient scaffolding template for modular ViewModels using C# 13 partial properties and <see cref="ToolViewModelBase"/>.
/// </summary>
public partial class __MODULE_NAME__ViewModel(I__MODULE_NAME__Service service) : ToolViewModelBase
{
    private readonly I__MODULE_NAME__Service _service = service ?? throw new ArgumentNullException(nameof(service));

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    public partial string InputData { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputData { get; set; } = string.Empty;

    private bool CanExecute => !string.IsNullOrWhiteSpace(InputData) && IsNotBusy;

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var output = await RunSafeAsync(
            ct => _service.ProcessAsync(InputData, ct),
            busyStatus: "Processing...",
            successStatus: "Completed.",
            cancellationToken: cancellationToken);

        if (output is not null)
        {
            OutputData = output;
        }
    }
}
