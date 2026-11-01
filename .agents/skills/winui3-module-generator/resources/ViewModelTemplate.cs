using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.__MODULE_NAME__;

/// <summary>
/// Scaffolding template for modular ViewModels using C# 13 partial properties.
/// </summary>
public partial class __MODULE_NAME__ViewModel : ObservableObject
{
    private readonly I__MODULE_NAME__Service _service;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    public partial string InputData { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputData { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    public partial bool IsBusy { get; set; }

    public __MODULE_NAME__ViewModel(I__MODULE_NAME__Service service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    private bool CanExecute => !string.IsNullOrWhiteSpace(InputData) && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _service.ProcessAsync(InputData, cancellationToken);
            if (result.IsSuccess)
            {
                OutputData = result.Value;
            }
            else
            {
                ErrorMessage = result.Error.Message;
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Operation cancelled by user.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

