using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.FolderCreator;

namespace OmniForge.App.Modules.BatchFolder;

/// <summary>
/// ViewModel governing the Batch Folder Creator module.
/// Manages input parsing, real-time debounced preview, collision resolution,
/// folder selection, and asynchronous creation with fault isolation.
/// </summary>
public partial class FolderCreatorViewModel : ToolViewModelBase, IDisposable
{
    private readonly IBatchFolderService _folderService;
    private readonly IFilePickerService _filePickerService;
    private readonly IDispatcherService _dispatcherService;
    private readonly IClipboardService? _clipboardService;

    private CancellationTokenSource? _debounceCts;
    private IReadOnlyList<FolderItemNode> _lastPlannedItems = [];

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
        GC.SuppressFinalize(this);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(RootDirectoryExists))]
    public partial string RootDirectory { get; set; } = string.Empty;

    public bool RootDirectoryExists => !string.IsNullOrWhiteSpace(RootDirectory) && Directory.Exists(RootDirectory);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    public partial string RawInputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LineNumbersText { get; set; } = "1";

    [ObservableProperty]
    public partial FolderInputMode InputMode { get; set; } = FolderInputMode.TreeHierarchy;

    [ObservableProperty]
    public partial bool IsTreeMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFlatMode { get; set; }

    [ObservableProperty]
    public partial FolderConflictResolution ConflictResolution { get; set; } = FolderConflictResolution.SkipExisting;

    [ObservableProperty]
    public partial int ConflictResolutionIndex { get; set; } = 0;

    [ObservableProperty]
    public partial int StartSequence { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalFoldersCount { get; set; }

    [ObservableProperty]
    public partial int NewFoldersCount { get; set; }

    [ObservableProperty]
    public partial int ExistingFoldersCount { get; set; }

    [ObservableProperty]
    public partial int ErrorsCount { get; set; }

    [ObservableProperty]
    public partial string DryRunSummaryText { get; set; } = "Enter folder hierarchy to preview.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreviewEmpty))]
    public partial bool HasPreviewItems { get; set; }

    public bool IsPreviewEmpty => !HasPreviewItems;

    [ObservableProperty]
    public partial string ExecutionResultSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasExecutionSummary { get; set; }

    [ObservableProperty]
    public partial string QuickAddFolderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool QuickAddAsSubfolder { get; set; }

    [ObservableProperty]
    public partial string SeriesPattern { get; set; } = "Lesson_{seq:2}";

    [ObservableProperty]
    public partial int SeriesCount { get; set; } = 10;

    [ObservableProperty]
    public partial bool AppendSeriesToExisting { get; set; } = true;

    [ObservableProperty]
    public partial string SeriesPreviewSampleText { get; set; } = "Preview: Lesson_01, Lesson_02 ... Lesson_10";

    [ObservableProperty]
    public partial string SearchFilterText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PrefixText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuffixText { get; set; } = string.Empty;

    // --- Import Modal States ---
    [ObservableProperty]
    public partial bool IsImportModalOpen { get; set; }

    [ObservableProperty]
    public partial int ImportTabIndex { get; set; }

    [ObservableProperty]
    public partial string ImportFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImportFileStatusText { get; set; } = "No file selected. Supports .xlsx, .csv, .tsv, .txt";

    [ObservableProperty]
    public partial string ImportPayloadText { get; set; } = string.Empty;

    public ObservableCollection<string> ImportPreviewItems { get; } = [];

    [ObservableProperty]
    public partial bool HasImportPreview { get; set; }

    [ObservableProperty]
    public partial string ImportPreviewSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsImportReplaceMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsImportAppendMode { get; set; }

    [ObservableProperty]
    public partial bool ImportSanitizeCharacters { get; set; } = true;

    [ObservableProperty]
    public partial string FormattedTreeDiagram { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTextDiagramView { get; set; }

    [ObservableProperty]
    public partial bool IsVisualTreeView { get; set; } = true;

    [ObservableProperty]
    public partial bool IsDiagramCopied { get; set; }

    public ObservableCollection<TreeViewNode> PreviewTreeNodes { get; } = [];

    public bool CanCreate => !string.IsNullOrWhiteSpace(RootDirectory) &&
                             !string.IsNullOrWhiteSpace(RawInputText) &&
                             TotalFoldersCount > 0 &&
                             IsNotBusy;

    public FolderCreatorViewModel(
        IBatchFolderService folderService,
        IFilePickerService filePickerService,
        IDispatcherService dispatcherService,
        IClipboardService? clipboardService = null)
    {
        _folderService = folderService ?? throw new ArgumentNullException(nameof(folderService));
        _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
        _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        _clipboardService = clipboardService;

        // Default to a safe user Documents subfolder
        string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        RootDirectory = Path.Combine(docs, "OmniForge_Folders");

        // Load default template for immediate user feedback
        LoadTemplate("project");
        UpdateSeriesPreviewSample();
    }

    partial void OnIsVisualTreeViewChanged(bool value)
    {
        if (value && IsTextDiagramView) IsTextDiagramView = false;
    }

    partial void OnIsTextDiagramViewChanged(bool value)
    {
        if (value && IsVisualTreeView) IsVisualTreeView = false;
    }

    [RelayCommand]
    private async Task CopyTreeDiagramAsync()
    {
        if (string.IsNullOrWhiteSpace(FormattedTreeDiagram)) return;

        if (_clipboardService is not null)
        {
            _clipboardService.CopyText(FormattedTreeDiagram);
        }
        else
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(FormattedTreeDiagram);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        IsDiagramCopied = true;
        await Task.Delay(2000).ConfigureAwait(true);
        IsDiagramCopied = false;
    }

    partial void OnRootDirectoryChanged(string value)
    {
        OnPropertyChanged(nameof(RootDirectoryExists));
        TriggerDebouncedPreview();
    }

    partial void OnSearchFilterTextChanged(string value) => TriggerDebouncedPreview();

    partial void OnSeriesPatternChanged(string value) => UpdateSeriesPreviewSample();

    partial void OnSeriesCountChanged(int value) => UpdateSeriesPreviewSample();

    partial void OnIsImportReplaceModeChanged(bool value)
    {
        if (value && IsImportAppendMode) IsImportAppendMode = false;
    }

    partial void OnIsImportAppendModeChanged(bool value)
    {
        if (value && IsImportReplaceMode) IsImportReplaceMode = false;
    }

    private void UpdateSeriesPreviewSample()
    {
        int count = Math.Clamp(SeriesCount, 1, 500);
        string pattern = string.IsNullOrWhiteSpace(SeriesPattern) ? "Folder_{seq:2}" : SeriesPattern.Trim();
        if (!TokenEvaluator.HasSequenceToken(pattern))
        {
            pattern = $"{pattern}_{{seq:2}}";
        }

        if (count <= 3)
        {
            var samples = Enumerable.Range(0, count)
                .Select(i => TokenEvaluator.Evaluate(pattern, StartSequence + i));
            SeriesPreviewSampleText = $"Preview: {string.Join(", ", samples)}";
        }
        else
        {
            string first = TokenEvaluator.Evaluate(pattern, StartSequence);
            string second = TokenEvaluator.Evaluate(pattern, StartSequence + 1);
            string last = TokenEvaluator.Evaluate(pattern, StartSequence + count - 1);
            SeriesPreviewSampleText = $"Preview: {first}, {second} ... {last} ({count} folders)";
        }
    }

    partial void OnRawInputTextChanged(string value)
    {
        UpdateLineNumbers(value);
        TriggerDebouncedPreview();
    }

    partial void OnInputModeChanged(FolderInputMode value)
    {
        IsTreeMode = value == FolderInputMode.TreeHierarchy;
        IsFlatMode = value == FolderInputMode.Flat;
        TriggerDebouncedPreview();
    }

    partial void OnIsTreeModeChanged(bool value)
    {
        if (value && InputMode != FolderInputMode.TreeHierarchy)
        {
            InputMode = FolderInputMode.TreeHierarchy;
        }
    }

    partial void OnIsFlatModeChanged(bool value)
    {
        if (value && InputMode != FolderInputMode.Flat)
        {
            InputMode = FolderInputMode.Flat;
        }
    }

    partial void OnConflictResolutionChanged(FolderConflictResolution value)
    {
        ConflictResolutionIndex = (int)value;
        TriggerDebouncedPreview();
    }

    partial void OnConflictResolutionIndexChanged(int value)
    {
        var target = value switch
        {
            1 => FolderConflictResolution.Overwrite,
            2 => FolderConflictResolution.AppendSuffix,
            _ => FolderConflictResolution.SkipExisting
        };

        if (ConflictResolution != target)
        {
            ConflictResolution = target;
        }
    }

    partial void OnStartSequenceChanged(int value) => TriggerDebouncedPreview();

    private void UpdateLineNumbers(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            LineNumbersText = "1";
            return;
        }

        int count = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                count++;
            }
        }

        var sb = new StringBuilder(count * 4);
        for (int i = 1; i <= count; i++)
        {
            sb.AppendLine(i.ToString(CultureInfo.InvariantCulture));
        }

        LineNumbersText = sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Debounces preview recalculation to keep typing fluid and responsive at 60+ FPS.
    /// </summary>
    public void TriggerDebouncedPreview()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(180, token).ConfigureAwait(false);
                await RecomputePreviewAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal when typing rapidly
            }
            catch (Exception ex)
            {
                _dispatcherService.TryEnqueue(() =>
                {
                    ErrorMessage = $"Preview calculation error: {ex.Message}";
                });
            }
        }, token);
    }

    private async Task RecomputePreviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(RawInputText) || string.IsNullOrWhiteSpace(RootDirectory))
        {
            _dispatcherService.TryEnqueue(() =>
            {
                PreviewTreeNodes.Clear();
                FormattedTreeDiagram = string.Empty;
                TotalFoldersCount = 0;
                NewFoldersCount = 0;
                ExistingFoldersCount = 0;
                ErrorsCount = 0;
                HasPreviewItems = false;
                DryRunSummaryText = "Enter folder names and choose target root folder.";
                OnPropertyChanged(nameof(CanCreate));
            });
            return;
        }

        var dryRunResult = _folderService.InspectDryRun(
            RootDirectory,
            RawInputText,
            InputMode,
            ConflictResolution,
            StartSequence);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (dryRunResult.IsFailure)
        {
            _dispatcherService.TryEnqueue(() =>
            {
                PreviewTreeNodes.Clear();
                FormattedTreeDiagram = string.Empty;
                TotalFoldersCount = 0;
                NewFoldersCount = 0;
                ExistingFoldersCount = 0;
                ErrorsCount = 1;
                HasPreviewItems = false;
                DryRunSummaryText = $"Preview Error: {dryRunResult.Error.Message}";
                ErrorMessage = dryRunResult.Error.Message;
                OnPropertyChanged(nameof(CanCreate));
            });
            return;
        }

        var result = dryRunResult.Value;
        _lastPlannedItems = result.AllItems;

        var displayedNodes = !string.IsNullOrWhiteSpace(SearchFilterText)
            ? FilterNodes(result.RootNodes, SearchFilterText.Trim())
            : result.RootNodes;

        string rootName = !string.IsNullOrWhiteSpace(RootDirectory)
            ? Path.GetFileName(RootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : "Root";
        if (string.IsNullOrWhiteSpace(rootName)) rootName = "Root";

        string diagram = FolderParser.GenerateTreeDiagram(rootName, displayedNodes, includeRoot: true, showStatusBadges: true);

        // TreeViewNode is a WinUI DependencyObject and MUST be created on the UI thread
        _dispatcherService.TryEnqueue(() =>
        {
            PreviewTreeNodes.Clear();
            foreach (var rootModel in displayedNodes)
            {
                PreviewTreeNodes.Add(FolderPreviewNode.FromModel(rootModel));
            }

            FormattedTreeDiagram = diagram;
            TotalFoldersCount = result.TotalCount;
            NewFoldersCount = result.NewCount;
            ExistingFoldersCount = result.ExistingCount;
            ErrorsCount = result.InvalidCount;
            HasPreviewItems = result.TotalCount > 0;
            ErrorMessage = result.ValidationErrors.Count > 0 ? string.Join("; ", result.ValidationErrors) : null;

            DryRunSummaryText = $"Planned: {result.TotalCount} folders ({result.NewCount} new, {result.ExistingCount} existing, {result.SuffixAppendedCount} renamed suffix).";
            OnPropertyChanged(nameof(CanCreate));
        });
    }

    private static List<FolderItemNode> FilterNodes(IReadOnlyList<FolderItemNode> nodes, string filter)
    {
        var matching = new List<FolderItemNode>();
        foreach (var node in nodes)
        {
            var filteredChildren = FilterNodes(node.Children, filter);
            bool selfMatches = node.EvaluatedName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                               node.RelativePath.Contains(filter, StringComparison.OrdinalIgnoreCase);

            if (selfMatches || filteredChildren.Count > 0)
            {
                var clone = new FolderItemNode
                {
                    RawText = node.RawText,
                    EvaluatedName = node.EvaluatedName,
                    RelativePath = node.RelativePath,
                    FullPath = node.FullPath,
                    IndentLevel = node.IndentLevel,
                    LineNumber = node.LineNumber,
                    Status = node.Status,
                    ErrorMessage = node.ErrorMessage
                };
                foreach (var c in filteredChildren) clone.AddChild(c);
                matching.Add(clone);
            }
        }
        return matching;
    }

    [RelayCommand]
    private async Task BrowseRootFolderAsync(CancellationToken cancellationToken)
    {
        var pickResult = await _filePickerService.PickFolderAsync(cancellationToken);
        if (pickResult.IsSuccess && !string.IsNullOrWhiteSpace(pickResult.Value))
        {
            RootDirectory = pickResult.Value;
        }
    }

    [RelayCommand]
    private void OpenRootFolder()
    {
        if (string.IsNullOrWhiteSpace(RootDirectory))
        {
            return;
        }

        try
        {
            if (!Directory.Exists(RootDirectory))
            {
                Directory.CreateDirectory(RootDirectory);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = RootDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private void InsertToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        RawInputText = string.IsNullOrEmpty(RawInputText) ? token : $"{RawInputText}{token}";
    }

    [RelayCommand]
    private void LoadTemplate(string templateType)
    {
        RawInputText = FolderParser.GetPresetTemplate(templateType);
        StatusMessage = $"Loaded '{templateType}' folder template.";
    }

    [RelayCommand]
    private void ClearInput()
    {
        if (string.IsNullOrWhiteSpace(RawInputText)) return;

        RequestConfirmation(
            title: "Confirm Clear Folder Structure",
            message: "Are you sure you want to clear all folder rows in the editor?",
            onConfirmedAction: _ =>
            {
                RawInputText = string.Empty;
                HasExecutionSummary = false;
                ExecutionResultSummary = string.Empty;
                SuccessMessage = "Cleared folder editor content.";
                return Task.CompletedTask;
            });
    }

    [RelayCommand]
    private void QuickAddFolder()
    {
        if (string.IsNullOrWhiteSpace(QuickAddFolderName))
        {
            return;
        }

        var newEntries = FolderParser.ParseBulkAddInput(QuickAddFolderName, QuickAddAsSubfolder);
        if (newEntries.Count == 0)
        {
            return;
        }

        string appendedText = string.Join(Environment.NewLine, newEntries);
        RawInputText = string.IsNullOrWhiteSpace(RawInputText)
            ? appendedText
            : $"{RawInputText.TrimEnd('\r', '\n')}{Environment.NewLine}{appendedText}";

        QuickAddFolderName = string.Empty;
        StatusMessage = $"Added {newEntries.Count} folder(s) to list.";
    }

    [RelayCommand]
    private void ApplyPrefixSuffix()
    {
        if (string.IsNullOrWhiteSpace(RawInputText)) return;
        if (string.IsNullOrEmpty(PrefixText) && string.IsNullOrEmpty(SuffixText)) return;

        RawInputText = FolderParser.ApplyPrefixSuffix(RawInputText, PrefixText, SuffixText);
        StatusMessage = $"Applied prefix '{PrefixText}' and suffix '{SuffixText}' to all folder rows.";
        PrefixText = string.Empty;
        SuffixText = string.Empty;
    }

    [RelayCommand]
    private void OpenImportModal()
    {
        IsImportModalOpen = true;
        ImportTabIndex = 0;
        ImportFilePath = string.Empty;
        ImportFileStatusText = "No file selected. Supports .xlsx, .csv, .tsv, .txt";
        ImportPreviewItems.Clear();
        HasImportPreview = false;
        ImportPreviewSummary = string.Empty;
        ImportPayloadText = string.Empty;
        IsImportReplaceMode = true;
        IsImportAppendMode = false;
        ImportSanitizeCharacters = true;
    }

    [RelayCommand]
    private void CloseImportModal()
    {
        IsImportModalOpen = false;
    }

    [RelayCommand]
    private async Task PickImportFileAsync(CancellationToken cancellationToken)
    {
        var pickResult = await _filePickerService.PickSingleFileAsync(
            [".xlsx", ".csv", ".tsv", ".txt"],
            cancellationToken);

        if (pickResult.IsFailure || string.IsNullOrWhiteSpace(pickResult.Value))
        {
            return;
        }

        string path = pickResult.Value;
        ImportFilePath = path;
        ImportFileStatusText = Path.GetFileName(path);

        var parseResult = await _folderService.ImportFolderListFromFileAsync(path, cancellationToken);
        if (parseResult.IsFailure)
        {
            ErrorMessage = parseResult.Error.Message;
            ImportFileStatusText = $"Error reading file: {parseResult.Error.Message}";
            ImportPreviewItems.Clear();
            HasImportPreview = false;
            return;
        }

        ImportPreviewItems.Clear();
        foreach (var item in parseResult.Value.Take(100))
        {
            ImportPreviewItems.Add(item);
        }

        HasImportPreview = parseResult.Value.Count > 0;
        ImportPreviewSummary = $"Loaded {parseResult.Value.Count} folder(s) ready to import (showing first {ImportPreviewItems.Count}):";
    }

    [RelayCommand]
    private async Task ExecuteImportAsync()
    {
        IReadOnlyList<string> itemsToImport = [];

        if (ImportTabIndex == 0) // File Tab
        {
            if (string.IsNullOrWhiteSpace(ImportFilePath) || !File.Exists(ImportFilePath))
            {
                ErrorMessage = "Please select a valid Excel or text file first.";
                return;
            }

            var result = await _folderService.ImportFolderListFromFileAsync(ImportFilePath);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }
            itemsToImport = result.Value;
        }
        else // Paste Tab
        {
            if (string.IsNullOrWhiteSpace(ImportPayloadText))
            {
                ErrorMessage = "Please paste or enter folder names first.";
                return;
            }

            var result = _folderService.ParseFolderListFromText(ImportPayloadText, ImportSanitizeCharacters);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }
            itemsToImport = result.Value;
        }

        if (itemsToImport.Count == 0)
        {
            ErrorMessage = "No valid folder items to import.";
            return;
        }

        string formattedText = string.Join(Environment.NewLine, itemsToImport);

        if (IsImportReplaceMode || string.IsNullOrWhiteSpace(RawInputText))
        {
            RawInputText = formattedText;
        }
        else
        {
            RawInputText = $"{RawInputText.TrimEnd('\r', '\n')}{Environment.NewLine}{formattedText}";
        }

        IsImportModalOpen = false;
        SuccessMessage = $"Successfully imported {itemsToImport.Count} folder item(s).";
    }

    [RelayCommand]
    private void GenerateSeries()
    {
        RawInputText = FolderParser.GenerateSeriesText(
            SeriesPattern,
            SeriesCount,
            StartSequence,
            AppendSeriesToExisting,
            RawInputText);

        StatusMessage = $"Generated {Math.Clamp(SeriesCount, 1, 500)} folders using pattern '{SeriesPattern}'.";
    }

    [RelayCommand]
    private void IndentAll()
    {
        RawInputText = FolderParser.AdjustIndentation(RawInputText, 2);
        StatusMessage = "Indented all lines (+2 spaces).";
    }

    [RelayCommand]
    private void OutdentAll()
    {
        RawInputText = FolderParser.AdjustIndentation(RawInputText, -2);
        StatusMessage = "Outdented all lines (-2 spaces).";
    }

    [RelayCommand]
    private void CleanAndSortLines(string? mode)
    {
        bool sort = string.Equals(mode, "sort", StringComparison.OrdinalIgnoreCase);
        RawInputText = FolderParser.NormalizeLines(RawInputText, sort);
        StatusMessage = sort ? "Cleaned and sorted lines alphabetically." : "Removed blank lines and trimmed whitespace.";
    }

    [RelayCommand]
    private void ExpandAllPreview()
    {
        SetExpansionRecursive(PreviewTreeNodes, true);
    }

    [RelayCommand]
    private void CollapseAllPreview()
    {
        SetExpansionRecursive(PreviewTreeNodes, false);
    }

    private static void SetExpansionRecursive(IEnumerable<TreeViewNode> nodes, bool isExpanded)
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = isExpanded;
            if (node.Children.Count > 0)
            {
                SetExpansionRecursive(node.Children, isExpanded);
            }
        }
    }

    [RelayCommand]
    private void RefreshPreview()
    {
        TriggerDebouncedPreview();
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void CreateFolders()
    {
        if (_lastPlannedItems.Count == 0)
        {
            ErrorMessage = "No folders planned for creation.";
            return;
        }

        RequestConfirmation(
            title: "Confirm Batch Folder Creation",
            message: $"Create {TotalFoldersCount} folder(s) ({NewFoldersCount} new, {ExistingFoldersCount} existing) inside '{RootDirectory}'?",
            onConfirmedAction: ExecuteCreateFoldersConfirmedAsync);
    }

    private async Task ExecuteCreateFoldersConfirmedAsync(CancellationToken cancellationToken)
    {
        HasExecutionSummary = false;
        ExecutionResultSummary = string.Empty;

        var progress = new Progress<BatchProgressReport>(report =>
        {
            ReportProgress(report.Percentage, $"Processing: {report.CurrentItemName} ({report.ProcessedItems}/{report.TotalItems})");
        });

        var summary = await RunSafeAsync(
            ct => _folderService.ExecuteCreationAsync(_lastPlannedItems, progress, ct),
            busyStatus: "Creating directories asynchronously...",
            successStatus: "Batch folder creation completed.",
            cancellationToken: cancellationToken);

        if (summary is not null)
        {
            HasExecutionSummary = true;
            string elapsedText = ByteSizeFormatter.FormatDuration(summary.Elapsed);
            ExecutionResultSummary = $"Successfully created {summary.CreatedCount} folders, skipped {summary.SkippedCount}, with {summary.FailedCount} errors in {elapsedText}.";
            SuccessMessage = ExecutionResultSummary;

            if (summary.Errors.Count > 0)
            {
                ErrorMessage = $"Encountered {summary.Errors.Count} errors:\n" + string.Join("\n", summary.Errors.Take(5));
            }

            // Refresh preview against the updated file system state
            TriggerDebouncedPreview();
        }
    }
}
