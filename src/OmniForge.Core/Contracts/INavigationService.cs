namespace OmniForge.Core.Contracts;

/// <summary>
/// Defines the shell navigation contract, decoupling ViewModels from concrete UI frame controls.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets a value indicating whether backward navigation is currently possible in the active frame stack.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets the Type of the currently displayed page.
    /// </summary>
    Type? CurrentPageType { get; }

    /// <summary>
    /// Navigates to a view resolved by its registered string key or module identifier.
    /// </summary>
    /// <param name="pageKey">The key or module ID representing the target view.</param>
    /// <param name="parameter">Optional parameter passed to the target view.</param>
    /// <param name="clearNavigationHistory">Whether to clear the frame back stack upon navigation.</param>
    /// <returns><c>true</c> if navigation succeeded; otherwise, <c>false</c>.</returns>
    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigationHistory = false);

    /// <summary>
    /// Navigates directly to a target Page Type.
    /// </summary>
    /// <param name="pageType">The Page Type to navigate to.</param>
    /// <param name="parameter">Optional parameter passed to the target view.</param>
    /// <param name="clearNavigationHistory">Whether to clear the frame back stack upon navigation.</param>
    /// <returns><c>true</c> if navigation succeeded; otherwise, <c>false</c>.</returns>
    bool NavigateTo(Type pageType, object? parameter = null, bool clearNavigationHistory = false);

    /// <summary>
    /// Navigates to the previous page in the navigation stack.
    /// </summary>
    /// <returns><c>true</c> if backward navigation succeeded; otherwise, <c>false</c>.</returns>
    bool GoBack();

    /// <summary>
    /// Event raised after a successful navigation transition has completed.
    /// </summary>
    event EventHandler<Type>? Navigated;
}

