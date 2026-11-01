using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace OmniForge.App.Modules.Checksum;

public sealed partial class ChecksumPage : Page
{
    public ChecksumViewModel ViewModel { get; } = App.GetService<ChecksumViewModel>();

    public ChecksumPage()
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
        try
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    ViewModel.TargetFilePath = items[0].Path;
                    await ViewModel.ComputeHashCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Error reading dropped file: {ex.Message}";
        }
    }
}
