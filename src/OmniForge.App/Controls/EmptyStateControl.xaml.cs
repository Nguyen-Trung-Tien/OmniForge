using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Controls;

/// <summary>
/// Reusable Fluent empty-state placeholder displayed when a collection or result view has no items.
/// </summary>
public sealed partial class EmptyStateControl : UserControl
{
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(EmptyStateControl), new PropertyMetadata("\uE8B7"));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateControl), new PropertyMetadata("No items to display"));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(EmptyStateControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsEmptyProperty =
        DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(EmptyStateControl), new PropertyMetadata(true));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public EmptyStateControl()
    {
        InitializeComponent();
    }
}

