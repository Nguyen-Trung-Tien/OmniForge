using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace OmniForge.App.Controls;

/// <summary>
/// Reusable Fluent Design card container with optional header icon, title, subtitle, action slot, and body content.
/// Eliminates repetitive Border + StackPanel boilerplate across all module XAML pages.
/// </summary>
[ContentProperty(Name = nameof(InnerContent))]
public sealed partial class SectionCardControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SectionCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SectionCardControl), new PropertyMetadata(string.Empty, OnSubtitleChanged));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(SectionCardControl), new PropertyMetadata(string.Empty, OnIconGlyphChanged));

    public static readonly DependencyProperty HeaderActionProperty =
        DependencyProperty.Register(nameof(HeaderAction), typeof(object), typeof(SectionCardControl), new PropertyMetadata(null));

    public static readonly DependencyProperty InnerContentProperty =
        DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(SectionCardControl), new PropertyMetadata(null));

    public static readonly DependencyProperty HasIconProperty =
        DependencyProperty.Register(nameof(HasIcon), typeof(bool), typeof(SectionCardControl), new PropertyMetadata(false));

    public static readonly DependencyProperty HasSubtitleProperty =
        DependencyProperty.Register(nameof(HasSubtitle), typeof(bool), typeof(SectionCardControl), new PropertyMetadata(false));

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

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public object? HeaderAction
    {
        get => GetValue(HeaderActionProperty);
        set => SetValue(HeaderActionProperty, value);
    }

    public object? InnerContent
    {
        get => GetValue(InnerContentProperty);
        set => SetValue(InnerContentProperty, value);
    }

    public bool HasIcon
    {
        get => (bool)GetValue(HasIconProperty);
        private set => SetValue(HasIconProperty, value);
    }

    public bool HasSubtitle
    {
        get => (bool)GetValue(HasSubtitleProperty);
        private set => SetValue(HasSubtitleProperty, value);
    }

    public SectionCardControl()
    {
        InitializeComponent();
    }

    private static void OnIconGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SectionCardControl control)
        {
            control.HasIcon = !string.IsNullOrWhiteSpace(e.NewValue as string);
        }
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SectionCardControl control)
        {
            control.HasSubtitle = !string.IsNullOrWhiteSpace(e.NewValue as string);
        }
    }
}

