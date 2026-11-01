using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Controls;

/// <summary>
/// Reusable Fluent header control for all OmniForge utility modules, eliminating repetitive XAML boilerplate.
/// </summary>
public sealed partial class ToolHeaderControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(null, OnErrorMessageChanged));

    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(ToolHeaderControl), new PropertyMetadata(false, OnHasErrorChanged));

    private static void OnErrorMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolHeaderControl control)
        {
            bool hasErr = !string.IsNullOrEmpty(e.NewValue as string);
            if (control.HasError != hasErr)
            {
                control.HasError = hasErr;
            }
        }
    }

    private static void OnHasErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolHeaderControl control && e.NewValue is false && !string.IsNullOrEmpty(control.ErrorMessage))
        {
            control.ErrorMessage = null;
        }
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

    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public ToolHeaderControl()
    {
        InitializeComponent();
    }
}

