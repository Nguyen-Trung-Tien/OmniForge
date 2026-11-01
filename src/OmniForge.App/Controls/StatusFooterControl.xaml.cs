using System.Globalization;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniForge.Core.Models;

namespace OmniForge.App.Controls;

/// <summary>
/// Reusable status bar, determinate/indeterminate progress indicator, and task cancellation control
/// binding directly to <see cref="ToolViewModelBase"/> properties.
/// </summary>
public sealed partial class StatusFooterControl : UserControl
{
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(true, OnIsIndeterminateChanged));

    public static readonly DependencyProperty IsDeterminateProperty =
        DependencyProperty.Register(nameof(IsDeterminate), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(false));

    public static readonly DependencyProperty ProgressPercentProperty =
        DependencyProperty.Register(nameof(ProgressPercent), typeof(double), typeof(StatusFooterControl), new PropertyMetadata(0.0, OnProgressPercentChanged));

    public static readonly DependencyProperty ProgressPercentTextProperty =
        DependencyProperty.Register(nameof(ProgressPercentText), typeof(string), typeof(StatusFooterControl), new PropertyMetadata("0%"));

    public static readonly DependencyProperty StatusMessageProperty =
        DependencyProperty.Register(nameof(StatusMessage), typeof(string), typeof(StatusFooterControl), new PropertyMetadata("Ready."));

    public static readonly DependencyProperty DurationTextProperty =
        DependencyProperty.Register(nameof(DurationText), typeof(string), typeof(StatusFooterControl), new PropertyMetadata(string.Empty, OnDurationChanged));

    public static readonly DependencyProperty HasDurationProperty =
        DependencyProperty.Register(nameof(HasDuration), typeof(bool), typeof(StatusFooterControl), new PropertyMetadata(false));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(StatusFooterControl), new PropertyMetadata(null));

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

    public bool IsDeterminate
    {
        get => (bool)GetValue(IsDeterminateProperty);
        private set => SetValue(IsDeterminateProperty, value);
    }

    public double ProgressPercent
    {
        get => (double)GetValue(ProgressPercentProperty);
        set => SetValue(ProgressPercentProperty, value);
    }

    public string ProgressPercentText
    {
        get => (string)GetValue(ProgressPercentTextProperty);
        private set => SetValue(ProgressPercentTextProperty, value);
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

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public StatusFooterControl()
    {
        InitializeComponent();
    }

    private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFooterControl control && e.NewValue is bool isIndeterminate)
        {
            control.IsDeterminate = !isIndeterminate;
        }
    }

    private static void OnProgressPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFooterControl control && e.NewValue is double pct)
        {
            control.ProgressPercentText = string.Create(CultureInfo.InvariantCulture, $"{pct:F0}%");
        }
    }

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFooterControl control)
        {
            control.HasDuration = !string.IsNullOrWhiteSpace(e.NewValue as string);
        }
    }

    private void CancelTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (CancelCommand is { } cmd && cmd.CanExecute(null))
        {
            cmd.Execute(null);
            return;
        }

        if (DataContext is ToolViewModelBase vm)
        {
            vm.CancelCurrentOperation();
        }
    }
}
