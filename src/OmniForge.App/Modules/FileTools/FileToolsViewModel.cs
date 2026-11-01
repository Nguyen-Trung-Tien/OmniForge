using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.FileTools;

public partial class FileToolsViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;

    [ObservableProperty]
    public partial string TargetDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready to inspect files.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public ObservableCollection<string> DiscoveredFiles { get; } = [];

    public FileToolsViewModel(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
    }

    [RelayCommand]
    private async Task ScanDirectoryAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(TargetDirectory))
        {
            ErrorMessage = "Please provide a valid directory path.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = $"Scanning directory: {TargetDirectory}...";
            DiscoveredFiles.Clear();

            var existsResult = await _fileSystemService.DirectoryExistsAsync(TargetDirectory, cancellationToken);
            if (existsResult.IsFailure || !existsResult.Value)
            {
                ErrorMessage = existsResult.IsFailure
                    ? existsResult.Error.Message
                    : $"Directory '{TargetDirectory}' does not exist.";
                StatusMessage = "Scan aborted.";
                return;
            }

            int count = 0;
            await foreach (var file in _fileSystemService.EnumerateFilesAsync(TargetDirectory, "*.*", SearchOption.TopDirectoryOnly, cancellationToken))
            {
                DiscoveredFiles.Add(Path.GetFileName(file));
                count++;
                if (count >= 100) break;
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
