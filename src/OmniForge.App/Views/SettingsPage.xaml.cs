using Microsoft.UI.Xaml.Controls;
using OmniForge.App.ViewModels;

namespace OmniForge.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
