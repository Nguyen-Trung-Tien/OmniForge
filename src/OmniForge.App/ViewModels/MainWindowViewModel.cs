using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;

namespace OmniForge.App.ViewModels;

/// <summary>
/// Root Shell ViewModel governing application chrome, navigation hierarchy, search filtering, and dynamic module switching.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IToolRegistryService _toolRegistry;
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private readonly IDispatcherService _dispatcherService;

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
    public partial string SearchPlaceholder { get; set; } = "Search utilities...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasShellError))]
    public partial string? ShellErrorMessage { get; set; }

    public bool HasShellError => !string.IsNullOrEmpty(ShellErrorMessage);

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public ObservableCollection<string> SearchSuggestions { get; } = [];

    public MainWindowViewModel(
        INavigationService navigationService,
        IToolRegistryService toolRegistry,
        ISettingsService settingsService,
        ILocalizationService localizationService,
        IDispatcherService dispatcherService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

        _toolRegistry.ModulesChanged += OnModulesChanged;
        _navigationService.Navigated += OnNavigated;
        _localizationService.LanguageChanged += OnLanguageChanged;

        AppTitle = _localizationService.GetString(LocalizationKeys.Nav_AppTitle);
        SearchPlaceholder = _localizationService.GetString(LocalizationKeys.Common_SearchPlaceholder);
        RefreshNavigationItems();
    }

    private void OnLanguageChanged(object? sender, OmniForge.Core.Models.AppLanguage e)
    {
        _dispatcherService.TryEnqueue(() =>
        {
            AppTitle = _localizationService.GetString(LocalizationKeys.Nav_AppTitle);
            SearchPlaceholder = _localizationService.GetString(LocalizationKeys.Common_SearchPlaceholder);
            RefreshNavigationItems();

            if (SelectedItem is NavigationItemViewModel currentNav)
            {
                currentNav.RefreshLocalizedText();
                CurrentSectionTitle = currentNav.Title;
            }
            else if (CurrentSectionTitle == "Settings" || CurrentSectionTitle == "Cài đặt")
            {
                CurrentSectionTitle = _localizationService.GetString(LocalizationKeys.Nav_Settings);
            }
        });
    }

    private void OnModulesChanged(object? sender, EventArgs e)
    {
        _dispatcherService.TryEnqueue(RefreshNavigationItems);
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
            m.Title.Equals(query, StringComparison.OrdinalIgnoreCase) ||
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
            var navItem = new NavigationItemViewModel(module, _localizationService);
            SearchSuggestions.Add(navItem.Title);

            if (string.IsNullOrWhiteSpace(filter) ||
                navItem.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                module.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                navItem.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                module.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                module.Category.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                NavigationItems.Add(navItem);
            }
        }

        // Navigate to the restored module or first available module
        if (SelectedItem is null && NavigationItems.Count > 0)
        {
            _ = RestoreInitialSelectionAsync();
        }
    }

    private async Task RestoreInitialSelectionAsync()
    {
        try
        {
            var lastModuleId = await _settingsService.GetLastOpenedModuleAsync();
            NavigationItemViewModel? targetItem = null;

            if (!string.IsNullOrEmpty(lastModuleId))
            {
                targetItem = NavigationItems.FirstOrDefault(i =>
                    i.Id.Equals(lastModuleId, StringComparison.OrdinalIgnoreCase));
            }

            targetItem ??= NavigationItems.FirstOrDefault();

            if (targetItem is not null)
            {
                SelectedItem = targetItem;
                TryNavigateToType(targetItem.ViewType, targetItem.Title);
            }
        }
        catch (Exception ex)
        {
            ShellErrorMessage = $"Failed to restore last module selection: {ex.Message}";
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

    private void OnNavigated(object? sender, Type pageType)
    {
        CanGoBack = _navigationService.CanGoBack;

        var matchingItem = NavigationItems.FirstOrDefault(i => i.ViewType == pageType);
        if (matchingItem is not null)
        {
            SelectedItem = matchingItem;
            CurrentSectionTitle = matchingItem.Title;
            _ = _settingsService.SetLastOpenedModuleAsync(matchingItem.Id);
        }
        else if (pageType.Name.Contains("Settings", StringComparison.OrdinalIgnoreCase))
        {
            CurrentSectionTitle = "Settings";
            _ = _settingsService.SetLastOpenedModuleAsync("Settings");
        }
    }
}
