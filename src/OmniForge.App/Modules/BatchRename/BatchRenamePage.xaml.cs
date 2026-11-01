using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace OmniForge.App.Modules.BatchRename;

/// <summary>
/// WinUI 3 Page for the Batch Rename tool module.
/// Pure code-behind with zero business logic adhering strictly to the AGENTS.md protocol.
/// </summary>
public sealed partial class BatchRenamePage : Page
{
    public BatchRenameViewModel ViewModel { get; } = App.GetService<BatchRenameViewModel>();

    public BatchRenamePage()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items.Select(i => i.Path).ToList();
            await ViewModel.ImportPathsAsync(paths);
        }
    }
}

