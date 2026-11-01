using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.FileTools;

public sealed record FileItemDisplayModel(
    string FileName,
    string Extension,
    long SizeBytes,
    string FormattedSize,
    string ModifiedText,
    string FullPath);

public partial class FileToolsViewModel(
    IFileSystemService fileSystemService,
    IFilePickerService? filePickerService = null,
    IClipboardService? clipboardService = null) : ToolViewModelBase
{
    private readonly IFileSystemService _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
    private readonly List<FileItemDisplayModel> _allScannedItems = [];

    [ObservableProperty]
    public partial string TargetDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    [ObservableProperty]
    public partial string SearchFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IncludeSubfolders { get; set; }

    [ObservableProperty]
    public partial string FormattedDirectorySize { get; set; } = "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFiles))]
    public partial bool IsEmpty { get; set; } = true;

    public bool HasFiles => !IsEmpty;

    [ObservableProperty]
    public partial int TotalScannedCount { get; set; }

    public BulkObservableCollection<string> DiscoveredFiles { get; } = [];

    public BulkObservableCollection<FileItemDisplayModel> DiscoveredFileItems { get; } = [];

    partial void OnSearchFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchFilter?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? _allScannedItems
            : _allScannedItems.Where(i =>
                i.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Extension.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        DiscoveredFileItems.ReplaceAll(filtered);
        DiscoveredFiles.ReplaceAll(filtered.Select(f => f.FileName));
        IsEmpty = filtered.Count == 0;
    }

    [RelayCommand]
    private async Task BrowseFolderAsync(CancellationToken cancellationToken)
    {
        if (filePickerService is null) return;

        var picked = await filePickerService.PickFolderAsync(cancellationToken);
        if (picked.IsSuccess && !string.IsNullOrWhiteSpace(picked.Value))
        {
            TargetDirectory = picked.Value;
            await ScanDirectoryAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private void CopyTargetPath()
    {
        if (clipboardService is null || string.IsNullOrWhiteSpace(TargetDirectory)) return;

        var res = clipboardService.CopyText(TargetDirectory);
        StatusMessage = res.IsSuccess ? "Copied directory path to clipboard." : res.Error.Message;
    }

    [RelayCommand]
    public void CopyFilePath(FileItemDisplayModel? item)
    {
        if (clipboardService is null || item is null) return;

        var res = clipboardService.CopyText(item.FullPath);
        StatusMessage = res.IsSuccess ? $"Copied '{item.FileName}' path to clipboard." : res.Error.Message;
    }

    [RelayCommand]
    public void RevealFileInExplorer(FileItemDisplayModel? item)
    {
        if (item is null || !File.Exists(item.FullPath))
        {
            ErrorMessage = "Selected file no longer exists on disk.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{item.FullPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to reveal file in Explorer: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrWhiteSpace(TargetDirectory) || !Directory.Exists(TargetDirectory))
        {
            ErrorMessage = "Target directory does not exist.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = TargetDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to open Explorer: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ScanDirectoryAsync(CancellationToken cancellationToken)
    {
        var pathCheck = SecurityGuard.ValidateSafePath(TargetDirectory);
        if (pathCheck.IsFailure)
        {
            ErrorMessage = pathCheck.Error.Message;
            return;
        }

        var exists = await RunSafeAsync(
            ct => _fileSystemService.DirectoryExistsAsync(pathCheck.Value, ct),
            busyStatus: $"Validating directory: {pathCheck.Value}...",
            cancellationToken: cancellationToken);

        if (!exists)
        {
            if (!HasError)
            {
                ErrorMessage = $"Directory '{TargetDirectory}' does not exist.";
                StatusMessage = "Scan aborted.";
            }
            return;
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        var linkedToken = BeginCancellableScope(cancellationToken);

        try
        {
            IsBusy = true;
            IsIndeterminate = true;
            ProgressPercent = 0;
            ClearError();
            SuccessMessage = null;
            StatusMessage = $"Scanning directory: {pathCheck.Value}...";

            var items = new List<FileItemDisplayModel>(capacity: 200);
            long totalBytes = 0;
            var searchOption = IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            await foreach (var file in _fileSystemService.EnumerateFilesAsync(pathCheck.Value, "*.*", searchOption, linkedToken))
            {
                linkedToken.ThrowIfCancellationRequested();

                var meta = FileMetadata.FromPath(file);
                totalBytes += meta.SizeBytes;
                items.Add(new FileItemDisplayModel(
                    FileName: meta.FileName,
                    Extension: string.IsNullOrEmpty(meta.Extension) ? "FILE" : meta.Extension.ToUpperInvariant(),
                    SizeBytes: meta.SizeBytes,
                    FormattedSize: ByteSizeFormatter.FormatBytes(meta.SizeBytes),
                    ModifiedText: meta.ModifiedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    FullPath: meta.FullPath));

                if (items.Count % 25 == 0)
                {
                    ReportProgress(Math.Min(99.0, items.Count / 5.0), $"Scanning ({items.Count} files found): {meta.FileName}");
                }

                if (items.Count >= 500) break;
            }

            _allScannedItems.Clear();
            _allScannedItems.AddRange(items.OrderByDescending(i => i.SizeBytes));
            TotalScannedCount = _allScannedItems.Count;
            FormattedDirectorySize = ByteSizeFormatter.FormatBytes(totalBytes);

            ApplyFilter();
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);
            string summary = $"Scan completed in {LastDurationText}. Found {TotalScannedCount} files ({FormattedDirectorySize}).";
            StatusMessage = summary;
            SuccessMessage = summary;
        }
        catch (OperationCanceledException)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            LastDurationText = ByteSizeFormatter.FormatDuration(elapsed);
            StatusMessage = $"File scan cancelled by user ({LastDurationText}).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error during file scan: {ex.Message}";
            StatusMessage = "Scan failed.";
        }
        finally
        {
            EndCancellableScope();
            IsBusy = false;
        }
    }
}
