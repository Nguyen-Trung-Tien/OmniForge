using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.FileTools;

public sealed partial class FileToolsPage : Page
{
    public FileToolsViewModel ViewModel { get; }

    public FileToolsPage()
    {
        ViewModel = App.GetService<FileToolsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void CopyFilePathButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FileItemDisplayModel item })
        {
            ViewModel.CopyFilePathCommand.Execute(item);
        }
    }

    private void RevealFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FileItemDisplayModel item })
        {
            ViewModel.RevealFileInExplorerCommand.Execute(item);
        }
    }
}
