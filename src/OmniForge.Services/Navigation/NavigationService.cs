using Microsoft.UI.Xaml.Controls;
using OmniForge.Core.Contracts;

namespace OmniForge.Services.Navigation;

/// <summary>
/// Production WinUI 3 implementation of <see cref="INavigationService"/> decoupling the UI Frame from ViewModels.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> _pageKeyMap = new(StringComparer.OrdinalIgnoreCase);
    private Frame? _frame;

    public event EventHandler<Type>? Navigated;

    /// <summary>
    /// Attaches the host WinUI 3 <see cref="Frame"/> to this navigation service.
    /// </summary>
    /// <param name="frame">The root frame located inside the shell.</param>
    public void Initialize(Frame frame)
    {
        if (_frame is not null)
        {
            _frame.Navigated -= OnFrameNavigated;
        }

        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        _frame.Navigated += OnFrameNavigated;
    }

    /// <summary>
    /// Maps a string key to a specific Page Type for named navigation.
    /// </summary>
    public void Configure(string key, Type pageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(pageType);

        _pageKeyMap[key] = pageType;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public Type? CurrentPageType => _frame?.CurrentSourcePageType;

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigationHistory = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        if (_pageKeyMap.TryGetValue(pageKey, out var mappedType))
        {
            return NavigateTo(mappedType, parameter, clearNavigationHistory);
        }

        return false;
    }

    public bool NavigateTo(Type pageType, object? parameter = null, bool clearNavigationHistory = false)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        if (_frame is null)
        {
            throw new InvalidOperationException("NavigationService has not been initialized with a Frame.");
        }

        // Avoid re-navigating if already on the same page
        if (_frame.Content is not null && _frame.CurrentSourcePageType == pageType)
        {
            return false;
        }

        bool navigated = _frame.Navigate(pageType, parameter);
        if (navigated && clearNavigationHistory)
        {
            _frame.BackStack.Clear();
        }

        return navigated;
    }

    public bool GoBack()
    {
        if (_frame is not null && _frame.CanGoBack)
        {
            _frame.GoBack();
            return true;
        }

        return false;
    }

    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.SourcePageType is not null)
        {
            Navigated?.Invoke(this, e.SourcePageType);
        }
    }
}

