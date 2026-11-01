using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniForge.App.ViewModels;
using OmniForge.Core.Contracts;
using OmniForge.Services.Navigation;

namespace OmniForge.App.Views;

/// <summary>
/// Root Shell Window providing fluent layout, custom titlebar, MicaAlt backdrop, and navigation host.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (navigationService is NavigationService navService)
        {
            navService.Initialize(ContentFrame);
        }

        // Trigger initial item binding and default navigation
        ViewModel.RefreshNavigationItems();
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        ViewModel.GoBackCommand.Execute(null);
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ViewModel.NavigateSettingsCommand.Execute(null);
        }
        else if (args.InvokedItemContainer?.DataContext is NavigationItemViewModel navItem)
        {
            ViewModel.NavigateItemCommand.Execute(navItem);
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.AutoSuggestQuerySubmittedCommand.Execute(args.QueryText);
    }
}
