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
}
