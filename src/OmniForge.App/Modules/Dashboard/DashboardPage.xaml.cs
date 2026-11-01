using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.Dashboard;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
