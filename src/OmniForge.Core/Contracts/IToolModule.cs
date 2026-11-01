namespace OmniForge.Core.Contracts;

/// <summary>
/// Defines the plugin interface for all OmniForge modular utility tools.
/// </summary>
public interface IToolModule
{
    /// <summary>
    /// Unique identifier for the module (e.g. "omniforge.dashboard", "omniforge.filetools").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// User-facing display name presented in the navigation pane and tool headers.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Backwards-compatible alias for <see cref="DisplayName"/>.
    /// </summary>
    string Title => DisplayName;

    /// <summary>
    /// Brief description summarizing the utility's capabilities.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Segoe Fluent Icon glyph or unicode character (e.g. "\uE80F").
    /// </summary>
    string IconGlyph { get; }

    /// <summary>
    /// Logical category for grouping inside navigation menus.
    /// </summary>
    string Category => "General";

    /// <summary>
    /// The runtime Type of the XAML View or Page associated with this tool.
    /// </summary>
    Type ViewType { get; }

    /// <summary>
    /// The runtime Type of the ViewModel resolved via the IoC container.
    /// </summary>
    Type ViewModelType { get; }

    /// <summary>
    /// Sorting priority within the navigation menu. Lower values appear first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Backwards-compatible alias for <see cref="Priority"/>.
    /// </summary>
    int Order => Priority;

    /// <summary>
    /// Indicates whether the module is active and available in navigation.
    /// </summary>
    bool IsEnabled => true;
}
