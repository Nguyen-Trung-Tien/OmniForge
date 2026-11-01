using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.FileTools;

public partial class FileToolsViewModel(IFileSystemService fileSystemService) : ToolViewModelBase
{
    private readonly IFileSystemService _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));

    [ObservableProperty]
    public partial string TargetDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public ObservableCollection<string> DiscoveredFiles { get; } = [];

    [RelayCommand]
    private async Task ScanDirectoryAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(TargetDirectory))
        {
            ErrorMessage = "Please provide a valid directory path.";
            return;
        }

        var exists = await RunSafeAsync(
            ct => _fileSystemService.DirectoryExistsAsync(TargetDirectory, ct),
            busyStatus: $"Validating directory: {TargetDirectory}...",
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
            DiscoveredFiles.Clear();
            StatusMessage = $"Scanning directory: {TargetDirectory}...";

            int count = 0;
            await foreach (var file in _fileSystemService.EnumerateFilesAsync(TargetDirectory, "*.*", SearchOption.TopDirectoryOnly, cancellationToken))
            {
                DiscoveredFiles.Add(Path.GetFileName(file));
                if (++count >= 100) break;
            }

            StatusMessage = $"Scan completed. Found {count} items.";
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
