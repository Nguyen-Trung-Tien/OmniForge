using Microsoft.UI.Xaml.Controls;

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
}

