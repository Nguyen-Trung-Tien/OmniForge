using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace OmniForge.App.Modules.BatchFolder;

/// <summary>
/// Code-behind for the Batch Folder Creator module page.
/// Complies with strict Zero Code-Behind MVVM architecture.
/// </summary>
public sealed partial class BatchFolderPage : Page
{
    public FolderCreatorViewModel ViewModel { get; } = App.GetService<FolderCreatorViewModel>();

    public BatchFolderPage()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void QuickAddTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.QuickAddFolderCommand.Execute(null);
            e.Handled = true;
        }
    }
}

