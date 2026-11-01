using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Example;

public sealed partial class HighPerformanceCollectionExample(IFileSystemService fileSystemService) : ToolViewModelBase
{
    public BulkObservableCollection<string> DiscoveredFiles { get; } = [];

    [RelayCommand]
    private async Task ScanFolderFastAsync(string rootPath, CancellationToken cancellationToken)
    {
        await RunSafeAsync(async ct =>
        {
            var buffer = new List<string>(capacity: 256);

            await foreach (var file in fileSystemService.EnumerateFilesAsync(rootPath, "*.*", SearchOption.AllDirectories, ct))
            {
                buffer.Add(file);
                if (buffer.Count >= 5000)
                {
                    break;
                }
            }

            // Single notification pass instead of 5,000 UI thread layout recalculations
            DiscoveredFiles.ReplaceAll(buffer);
            return Result.Success();
        },
        busyStatus: "Scanning directory at 60 FPS...",
        successStatus: "Scan complete.",
        cancellationToken: cancellationToken);
    }
}

