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
    [NotifyPropertyChangedFor(nameof(InputStatsText))]
    public partial string InputText { get; set; } = string.Empty;

    public bool HasInput => !string.IsNullOrEmpty(InputText);

    public string InputStatsText => string.IsNullOrEmpty(InputText)
        ? "0 chars | 0 lines"
        : $"{InputText.Length:N0} chars | {CountLines(InputText):N0} lines";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput))]
    [NotifyPropertyChangedFor(nameof(OutputStatsText))]
    public partial string OutputText { get; set; } = string.Empty;

    public bool HasOutput => !string.IsNullOrEmpty(OutputText);

    public string OutputStatsText => string.IsNullOrEmpty(OutputText)
        ? "0 chars | 0 lines"
        : $"{OutputText.Length:N0} chars | {CountLines(OutputText):N0} lines";

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int lines = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') lines++;
        }
        return lines;
    }

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

    [ObservableProperty]
    public partial bool IsDiffEmpty { get; set; } = true;

    public bool HasDiffResults => !IsDiffEmpty;

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
        IsDiffEmpty = diff.Count == 0;
        OnPropertyChanged(nameof(HasDiffResults));
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
        IsDiffEmpty = true;
        OnPropertyChanged(nameof(HasDiffResults));
        ClearError();
        StatusMessage = "Cleared.";
    }

    [RelayCommand]
    private void SwapInputOutput()
    {
        if (string.IsNullOrEmpty(OutputText)) return;
        (InputText, OutputText) = (OutputText, string.Empty);
        StatusMessage = "Swapped output into input.";
    }

    [RelayCommand]
    private void LoadSample()
    {
        ClearError();
        switch (SelectedModeIndex)
        {
            case 0: // JSON
                InputText = """{"project":"OmniForge","version":"0.2.0","modules":["BatchRename","BatchFolder","Checksum","TextInspector"],"active":true}""";
                _ = FormatJsonAsync("indented", CancellationToken.None);
                break;
            case 1: // XML
                InputText = """<OmniForge version="0.2.0"><Modules><Module id="json" enabled="true"/><Module id="xml" enabled="true"/></Modules></OmniForge>""";
                _ = FormatXmlAsync(CancellationToken.None);
                break;
            case 2: // Base64
                InputText = "OmniForge High-Performance Utility Suite";
                EncodeBase64();
                break;
            case 3: // URL
                InputText = "https://example.com/search?q=OmniForge Utility & filter=a+b";
                EncodeUrl();
                break;
            case 4: // JWT
                InputText = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ik9tbmlGb3JnZSBEZXYiLCJpYXQiOjE3MzAwMDAwMDB9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
                InspectJwt();
                break;
            case 5: // Diff
                InputText = "OmniForge v0.1.0\nModule: TextInspector\nLayout: Vertical Stack\nStatus: Legacy";
                SecondaryInputText = "OmniForge v0.2.0\nModule: TextInspector\nLayout: Side-by-Side Split View\nStatus: Optimized";
                ComputeDiff();
                break;
        }
    }
}
