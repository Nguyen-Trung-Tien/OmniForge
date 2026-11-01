using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.TextInspector;

public sealed partial class TextInspectorPage : Page
{
    public TextInspectorViewModel ViewModel { get; } = App.GetService<TextInspectorViewModel>();

    public TextInspectorPage()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }
}
