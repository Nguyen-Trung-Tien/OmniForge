using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.FileTools;

public partial class FileToolsViewModel(
    IFileSystemService fileSystemService,
    IFilePickerService? filePickerService = null,
    IClipboardService? clipboardService = null) : ToolViewModelBase
{
    private readonly IFileSystemService _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));

    [ObservableProperty]
    public partial string TargetDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    public partial string FormattedDirectorySize { get; set; } = "—";

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public BulkObservableCollection<string> DiscoveredFiles { get; } = [];

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

        try
        {
            IsBusy = true;
            StatusMessage = $"Scanning directory: {pathCheck.Value}...";

            var batch = new List<string>(capacity: 100);
            await foreach (var file in _fileSystemService.EnumerateFilesAsync(pathCheck.Value, "*.*", SearchOption.TopDirectoryOnly, cancellationToken))
            {
                batch.Add(Path.GetFileName(file));
                if (batch.Count >= 100) break;
            }

            DiscoveredFiles.ReplaceAll(batch);
            IsEmpty = DiscoveredFiles.Count == 0;
            var sizeResult = await _fileSystemService.GetDirectorySizeAsync(pathCheck.Value, cancellationToken);
            if (sizeResult.IsSuccess)
            {
                FormattedDirectorySize = ByteSizeFormatter.FormatBytes(sizeResult.Value);
            }

            StatusMessage = $"Scan completed. Found {batch.Count} items ({FormattedDirectorySize}).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "File scan was canceled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error during file scan: {ex.Message}";
            StatusMessage = "Scan failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
