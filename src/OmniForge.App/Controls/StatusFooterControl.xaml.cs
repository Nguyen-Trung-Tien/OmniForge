using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Controls;

/// <summary>
/// Reusable status bar and progress indicator binding directly to <see cref="OmniForge.Core.Models.ToolViewModelBase"/> properties.
/// </summary>
public sealed partial class StatusFooterControl : UserControl
{
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(true));

    public static readonly DependencyProperty ProgressPercentProperty =
        DependencyProperty.Register(nameof(ProgressPercent), typeof(double), typeof(StatusFooterControl), new PropertyMetadata(0.0));

    public static readonly DependencyProperty StatusMessageProperty =
        DependencyProperty.Register(nameof(StatusMessage), typeof(string), typeof(StatusFooterControl), new PropertyMetadata("Ready."));

    public static readonly DependencyProperty DurationTextProperty =
        DependencyProperty.Register(nameof(DurationText), typeof(string), typeof(StatusFooterControl), new PropertyMetadata(string.Empty, OnDurationChanged));

    public static readonly DependencyProperty HasDurationProperty =
        DependencyProperty.Register(nameof(HasDuration), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(false));

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public double ProgressPercent
    {
        get => (double)GetValue(ProgressPercentProperty);
        set => SetValue(ProgressPercentProperty, value);
    }

    public string StatusMessage
    {
        get => (string)GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    public string DurationText
    {
        get => (string)GetValue(DurationTextProperty);
        set => SetValue(DurationTextProperty, value);
    }

    public bool HasDuration
    {
        get => (bool)GetValue(HasDurationProperty);
        private set => SetValue(HasDurationProperty, value);
    }

    public StatusFooterControl()
    {
        InitializeComponent();
    }

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFooterControl control)
        {
            control.HasDuration = !string.IsNullOrWhiteSpace(e.NewValue as string);
        }
    }
}

