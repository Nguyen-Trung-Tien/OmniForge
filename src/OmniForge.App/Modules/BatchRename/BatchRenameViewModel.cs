using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Modules.BatchOps;

namespace OmniForge.App.Modules.BatchRename;

/// <summary>
/// ViewModel orchestrating file picking, drag-and-drop, chained rule pipeline editing,
/// live debounced collision preview calculation, and non-blocking batch execution with rollback undo.
/// </summary>
public sealed partial class BatchRenameViewModel : ToolViewModelBase, IDisposable
{
    private readonly IBatchRenameService _renameService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IFilePickerService? _filePickerService;
    private readonly IClipboardService? _clipboardService;
    private readonly IDispatcherService? _dispatcherService;

    private readonly List<FileMetadata> _loadedFiles = [];
    private CancellationTokenSource? _debounceCts;
    private RenameExecutionJournal? _lastJournal;

    public ObservableCollection<RuleItemViewModel> Rules { get; } = [];

    public BulkObservableCollection<RenamePreviewItem> PreviewItems { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    public partial int TotalFilesCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    public partial int ChangedFilesCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflicts))]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    public partial int ConflictCount { get; set; }

    public bool HasConflicts => ConflictCount > 0;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool HasFiles { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    public partial string UndoStatusText { get; set; } = "Undo";

    public bool CanRun => ChangedFilesCount > 0 && !HasConflicts && IsNotBusy;

    public bool CanUndo => _lastJournal is not null && _lastJournal.CompletedOperations.Count > 0 && IsNotBusy;

    public BatchRenameViewModel(
        IBatchRenameService renameService,
        IFileSystemService fileSystemService,
        IFilePickerService? filePickerService = null,
        IClipboardService? clipboardService = null,
        IDispatcherService? dispatcherService = null)
    {
        _renameService = renameService ?? throw new ArgumentNullException(nameof(renameService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _filePickerService = filePickerService;
        _clipboardService = clipboardService;
        _dispatcherService = dispatcherService;

        // Default with one sensible Prefix/Suffix rule
        Rules.Add(CreateRule(RenameRuleType.PrefixSuffix));
    }

    private RuleItemViewModel CreateRule(RenameRuleType type)
    {
        return new RuleItemViewModel(
            type,
            onRuleChanged: TriggerDebouncePreview,
            onRemove: RemoveRule,
            onMoveUp: MoveRuleUp,
            onMoveDown: MoveRuleDown);
    }

    /// <summary>
    /// Schedules a debounced preview recalculation throttled at 200ms to keep typing completely fluid.
    /// </summary>
    public void TriggerDebouncePreview()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(200, token).ConfigureAwait(false);
                await RecomputePreviewAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled by newer edit
            }
            catch (Exception ex)
            {
                _dispatcherService?.TryEnqueue(() =>
                {
                    ErrorMessage = $"Preview calculation error: {ex.Message}";
                });
            }
        }, token);
    }

    private async Task RecomputePreviewAsync(CancellationToken cancellationToken)
    {
        if (_loadedFiles.Count == 0)
        {
            UpdatePreviewUi([], 0, 0, 0, isEmpty: true, hasFiles: false);
            return;
        }

        var activeRules = Rules.Select(r => r.ToRule()).ToList();
        var previewResult = _renameService.BuildPreview(_loadedFiles, activeRules);

        if (previewResult.IsSuccess)
        {
            var items = previewResult.Value;
            UpdatePreviewUi(
                items,
                total: items.Count,
                changed: items.Count(i => i.HasChange),
                conflicts: items.Count(i => i.HasConflict),
                isEmpty: items.Count == 0,
                hasFiles: items.Count > 0);
        }
    }

    private void UpdatePreviewUi(
        IReadOnlyList<RenamePreviewItem> items,
        int total,
        int changed,
        int conflicts,
        bool isEmpty,
        bool hasFiles)
    {
        void Apply()
        {
            PreviewItems.ReplaceAll(items);
            TotalFilesCount = total;
            ChangedFilesCount = changed;
            ConflictCount = conflicts;
            IsEmpty = isEmpty;
            HasFiles = hasFiles;
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(HasConflicts));
        }

        if (_dispatcherService is not null)
        {
            _dispatcherService.TryEnqueue(Apply);
        }
        else
        {
            Apply();
        }
    }

    [RelayCommand]
    private async Task AddFilesAsync(CancellationToken cancellationToken)
    {
        if (_filePickerService is null) return;

        var picked = await _filePickerService.PickMultipleFilesAsync(cancellationToken: cancellationToken);
        if (picked.IsSuccess && picked.Value.Count > 0)
        {
            await ImportPathsAsync(picked.Value, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task AddFolderAsync(CancellationToken cancellationToken)
    {
        if (_filePickerService is null) return;

        var picked = await _filePickerService.PickFolderAsync(cancellationToken);
        if (picked.IsSuccess && !string.IsNullOrWhiteSpace(picked.Value))
        {
            await ImportPathsAsync([picked.Value], cancellationToken);
        }
    }

    [RelayCommand]
    private void ClearFiles()
    {
        _loadedFiles.Clear();
        PreviewItems.ReplaceAll([]);
        TotalFilesCount = 0;
        ChangedFilesCount = 0;
        ConflictCount = 0;
        IsEmpty = true;
        HasFiles = false;
        StatusMessage = "Cleared all imported files.";
        OnPropertyChanged(nameof(CanRun));
    }

    /// <summary>
    /// Ingests files or directory paths from file dialogs or Drag-and-Drop.
    /// Safely handles directory traversal and deduplication.
    /// </summary>
    public async Task ImportPathsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var existingPaths = new HashSet<string>(_loadedFiles.Select(f => f.FullPath), StringComparer.OrdinalIgnoreCase);
        var newlyFound = new List<string>();

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;

            if (Directory.Exists(path))
            {
                await foreach (var file in _fileSystemService.EnumerateFilesAsync(path, "*.*", SearchOption.TopDirectoryOnly, cancellationToken))
                {
                    if (existingPaths.Add(file))
                    {
                        newlyFound.Add(file);
                    }
                }
            }
            else if (File.Exists(path) && existingPaths.Add(path))
            {
                newlyFound.Add(path);
            }
        }

        foreach (var filePath in newlyFound)
        {
            _loadedFiles.Add(FileMetadata.FromPath(filePath));
        }

        StatusMessage = $"Imported {newlyFound.Count} file(s). Total: {_loadedFiles.Count}.";
        TriggerDebouncePreview();
    }

    [RelayCommand]
    private void AddRule(string? typeName)
    {
        var ruleType = typeName switch
        {
            "Replace" => RenameRuleType.Replace,
            "ChangeCase" => RenameRuleType.ChangeCase,
            "Numbering" => RenameRuleType.Numbering,
            _ => RenameRuleType.PrefixSuffix
        };

        Rules.Add(CreateRule(ruleType));
        TriggerDebouncePreview();
    }

    [RelayCommand]
    private void RemoveRule(RuleItemViewModel rule)
    {
        if (rule is not null && Rules.Remove(rule))
        {
            TriggerDebouncePreview();
        }
    }

    [RelayCommand]
    private void MoveRuleUp(RuleItemViewModel rule)
    {
        int index = Rules.IndexOf(rule);
        if (index > 0)
        {
            Rules.Move(index, index - 1);
            TriggerDebouncePreview();
        }
    }

    [RelayCommand]
    private void MoveRuleDown(RuleItemViewModel rule)
    {
        int index = Rules.IndexOf(rule);
        if (index >= 0 && index < Rules.Count - 1)
        {
            Rules.Move(index, index + 1);
            TriggerDebouncePreview();
        }
    }

    [RelayCommand]
    private async Task RunRenameAsync(CancellationToken cancellationToken)
    {
        if (!CanRun) return;

        var progress = new Progress<BatchProgressReport>(p =>
        {
            ReportProgress(p.Percentage, $"Renaming ({p.ProcessedItems}/{p.TotalItems}): {p.CurrentItemName}");
        });

        var result = await RunSafeAsync(
            ct => _renameService.ExecuteBatchAsync(PreviewItems, progress, ct),
            busyStatus: $"Executing batch rename ({ChangedFilesCount} files)...",
            successStatus: "Batch rename completed successfully.",
            cancellationToken: cancellationToken);

        if (result is not null)
        {
            _lastJournal = result;
            UndoStatusText = $"Undo ({result.CompletedOperations.Count})";
            OnPropertyChanged(nameof(CanUndo));

            // Update loaded files to reflect their new disk paths
            var completedLookup = result.CompletedOperations.ToDictionary(
                op => op.OriginalPath,
                op => op.RenamedPath,
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _loadedFiles.Count; i++)
            {
                var original = _loadedFiles[i];
                if (completedLookup.TryGetValue(original.FullPath, out var newPath))
                {
                    _loadedFiles[i] = FileMetadata.FromPath(newPath);
                }
            }

            // Recalculate preview against the new filenames
            TriggerDebouncePreview();
        }
    }

    [RelayCommand]
    private async Task UndoRenameAsync(CancellationToken cancellationToken)
    {
        if (!CanUndo || _lastJournal is null) return;

        var journal = _lastJournal;
        var result = await RunSafeAsync(
            ct => _renameService.UndoBatchAsync(journal, ct),
            busyStatus: $"Reverting {journal.CompletedOperations.Count} file(s)...",
            successStatus: "Undo completed successfully.",
            cancellationToken: cancellationToken);

        if (result > 0)
        {
            // Reverse the paths in loaded files
            var undoLookup = journal.CompletedOperations.ToDictionary(
                op => op.RenamedPath,
                op => op.OriginalPath,
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _loadedFiles.Count; i++)
            {
                var renamed = _loadedFiles[i];
                if (undoLookup.TryGetValue(renamed.FullPath, out var origPath))
                {
                    _loadedFiles[i] = FileMetadata.FromPath(origPath);
                }
            }

            _lastJournal = null;
            UndoStatusText = "Undo";
            OnPropertyChanged(nameof(CanUndo));
            TriggerDebouncePreview();
        }
    }

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }
}
