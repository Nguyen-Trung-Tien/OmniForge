using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.TextInspector;

namespace OmniForge.App.Modules.TextInspector;

public partial class TextInspectorViewModel : ToolViewModelBase
{
    private readonly ITextInspectorService _textService;
    private readonly IClipboardService? _clipboardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInput))]
    public partial string InputText { get; set; } = string.Empty;

    public bool HasInput => !string.IsNullOrEmpty(InputText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput))]
    public partial string OutputText { get; set; } = string.Empty;

    public bool HasOutput => !string.IsNullOrEmpty(OutputText);

    [ObservableProperty]
    public partial string SecondaryInputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiffMode))]
    [NotifyPropertyChangedFor(nameof(IsNotDiffMode))]
    [NotifyPropertyChangedFor(nameof(IsJsonMode))]
    [NotifyPropertyChangedFor(nameof(IsXmlMode))]
    [NotifyPropertyChangedFor(nameof(IsBase64Mode))]
    [NotifyPropertyChangedFor(nameof(IsUrlMode))]
    [NotifyPropertyChangedFor(nameof(IsJwtMode))]
    public partial int SelectedModeIndex { get; set; }

    public bool IsDiffMode => SelectedModeIndex == 5;
    public bool IsNotDiffMode => SelectedModeIndex != 5;
    public bool IsJsonMode => SelectedModeIndex == 0;
    public bool IsXmlMode => SelectedModeIndex == 1;
    public bool IsBase64Mode => SelectedModeIndex == 2;
    public bool IsUrlMode => SelectedModeIndex == 3;
    public bool IsJwtMode => SelectedModeIndex == 4;

    public BulkObservableCollection<DiffLine> DiffResults { get; } = [];

    public TextInspectorViewModel(
        ITextInspectorService textService,
        IClipboardService? clipboardService = null)
    {
        _textService = textService ?? throw new ArgumentNullException(nameof(textService));
        _clipboardService = clipboardService;
    }



    [RelayCommand]
    private async Task FormatJsonAsync(string? mode, CancellationToken cancellationToken)
    {
        bool indented = mode != "minify";

        var res = await RunSafeAsync(
            ct => _textService.FormatJsonAsync(InputText, indented, ct),
            busyStatus: indented ? "Formatting JSON..." : "Minifying JSON...",
            successStatus: indented ? "JSON formatted successfully." : "JSON minified.",
            cancellationToken: cancellationToken);

        if (res is not null)
        {
            OutputText = res;
        }
    }

    [RelayCommand]
    private async Task FormatXmlAsync(CancellationToken cancellationToken)
    {
        var res = await RunSafeAsync(
            ct => _textService.FormatXmlAsync(InputText, ct),
            busyStatus: "Formatting XML...",
            successStatus: "XML formatted successfully.",
            cancellationToken: cancellationToken);

        if (res is not null)
        {
            OutputText = res;
        }
    }

    [RelayCommand]
    private void EncodeBase64()
    {
        var res = _textService.EncodeBase64(InputText);
        if (res.IsSuccess)
        {
            OutputText = res.Value;
            StatusMessage = "Base64 encoded.";
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = res.Error.Message;
        }
    }

    [RelayCommand]
    private void DecodeBase64()
    {
        var res = _textService.DecodeBase64(InputText);
        if (res.IsSuccess)
        {
            OutputText = res.Value;
            StatusMessage = "Base64 decoded.";
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = res.Error.Message;
        }
    }

    [RelayCommand]
    private void EncodeUrl()
    {
        var res = _textService.EncodeUrl(InputText);
        if (res.IsSuccess)
        {
            OutputText = res.Value;
            StatusMessage = "URL encoded.";
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = res.Error.Message;
        }
    }

    [RelayCommand]
    private void DecodeUrl()
    {
        var res = _textService.DecodeUrl(InputText);
        if (res.IsSuccess)
        {
            OutputText = res.Value;
            StatusMessage = "URL decoded.";
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = res.Error.Message;
        }
    }

    [RelayCommand]
    private void InspectJwt()
    {
        var res = _textService.InspectJwt(InputText);
        if (res.IsSuccess)
        {
            OutputText = $"// --- HEADER ---\n{res.Value.HeaderJson}\n\n// --- PAYLOAD ---\n{res.Value.PayloadJson}\n\n// --- SIGNATURE ---\n{res.Value.Signature}";
            StatusMessage = "JWT parsed and formatted.";
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = res.Error.Message;
        }
    }

    [RelayCommand]
    private void ComputeDiff()
    {
        var diff = _textService.ComputeLineDiff(InputText, SecondaryInputText);
        DiffResults.ReplaceAll(diff);
        StatusMessage = $"Diff computed: {diff.Count} lines analyzed.";
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (_clipboardService is null || string.IsNullOrEmpty(OutputText)) return;

        var res = _clipboardService.CopyText(OutputText);
        StatusMessage = res.IsSuccess ? "Copied output to clipboard." : res.Error.Message;
    }

    [RelayCommand]
    private async Task PasteInputAsync()
    {
        if (_clipboardService is null) return;

        var res = await _clipboardService.GetTextAsync();
        if (res.IsSuccess && !string.IsNullOrEmpty(res.Value))
        {
            InputText = res.Value;
            StatusMessage = "Pasted from clipboard.";
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        SecondaryInputText = string.Empty;
        DiffResults.ReplaceAll([]);
        StatusMessage = "Cleared.";
    }
}
