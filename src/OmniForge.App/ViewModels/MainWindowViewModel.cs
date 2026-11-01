using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;

namespace OmniForge.App.ViewModels;

/// <summary>
/// Root Shell ViewModel governing application chrome, navigation hierarchy, search filtering, and dynamic module switching.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IToolRegistryService _toolRegistry;

    [ObservableProperty]
    public partial string AppTitle { get; set; } = "OmniForge Utility Suite";

    [ObservableProperty]
    public partial string CurrentSectionTitle { get; set; } = "Dashboard";

    [ObservableProperty]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    public partial object? SelectedItem { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasShellError))]
    public partial string? ShellErrorMessage { get; set; }

    public bool HasShellError => !string.IsNullOrEmpty(ShellErrorMessage);

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public ObservableCollection<string> SearchSuggestions { get; } = [];

    public MainWindowViewModel(INavigationService navigationService, IToolRegistryService toolRegistry)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));

        _toolRegistry.ModulesChanged += OnModulesChanged;
        _navigationService.Navigated += OnNavigated;

        RefreshNavigationItems();
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterNavigationItems(value);
    }

    [RelayCommand]
    private void DismissShellError()
    {
        ShellErrorMessage = null;
    }

    [RelayCommand]
    private void GoBack()
    {
        try
        {
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ShellErrorMessage = $"Navigation back failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NavigateItem(object? item)
    {
        if (item is NavigationItemViewModel navItem)
        {
            TryNavigateToType(navItem.ViewType, navItem.Title);
        }
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        try
        {
            ShellErrorMessage = null;
            _navigationService.NavigateTo("Settings");
        }
        catch (Exception ex)
        {
            ShellErrorMessage = $"Failed to open Settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AutoSuggestQuerySubmitted(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        var match = _toolRegistry.Modules.FirstOrDefault(m =>
            m.DisplayName.Equals(query, StringComparison.OrdinalIgnoreCase) ||
            m.Id.Equals(query, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            TryNavigateToType(match.ViewType, match.DisplayName);
        }
    }

    public void RefreshNavigationItems()
    {
        FilterNavigationItems(SearchQuery);
    }

    private void FilterNavigationItems(string filter)
    {
        NavigationItems.Clear();
        SearchSuggestions.Clear();

        var activeModules = _toolRegistry.Modules.Where(m => m.IsEnabled);

        foreach (var module in activeModules)
        {
            SearchSuggestions.Add(module.DisplayName);

            if (string.IsNullOrWhiteSpace(filter) ||
                module.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                module.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                module.Category.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                NavigationItems.Add(new NavigationItemViewModel(module));
            }
        }

        // Navigate to the first available module if nothing is currently selected
        if (SelectedItem is null && NavigationItems.Count > 0)
        {
            SelectedItem = NavigationItems[0];
            TryNavigateToType(NavigationItems[0].ViewType, NavigationItems[0].Title);
        }
    }

    private void TryNavigateToType(Type viewType, string moduleTitle)
    {
        try
        {
            ShellErrorMessage = null;
            _navigationService.NavigateTo(viewType);
        }
        catch (Exception ex)
        {
            ShellErrorMessage = $"Module '{moduleTitle}' failed to activate: {ex.Message}";
        }
    }

    private void OnModulesChanged(object? sender, EventArgs e)
    {
        RefreshNavigationItems();
    }

    private void OnNavigated(object? sender, Type pageType)
    {
        CanGoBack = _navigationService.CanGoBack;

        var matchingItem = NavigationItems.FirstOrDefault(i => i.ViewType == pageType);
        if (matchingItem is not null)
        {
            SelectedItem = matchingItem;
            CurrentSectionTitle = matchingItem.Title;
        }
        else if (pageType.Name.Contains("Settings", StringComparison.OrdinalIgnoreCase))
        {
            CurrentSectionTitle = "Settings";
        }
    }
}
