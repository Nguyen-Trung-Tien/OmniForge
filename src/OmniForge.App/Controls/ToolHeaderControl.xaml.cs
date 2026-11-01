using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniForge.Core.Models;

namespace OmniForge.App.Controls;

/// <summary>
/// Reusable Fluent header control for all OmniForge utility modules, eliminating repetitive XAML boilerplate.
/// Automatically binds to <see cref="ToolViewModelBase"/> for pre-action confirmation prompts, post-action completion banners, and fault-isolated error reporting.
/// </summary>
public sealed partial class ToolHeaderControl : UserControl
{
    private ToolViewModelBase? _subscribedVm;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(null, OnErrorMessageChanged));

    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(ToolHeaderControl), new PropertyMetadata(false, OnHasErrorChanged));

    public static readonly DependencyProperty SuccessMessageProperty =
        DependencyProperty.Register(nameof(SuccessMessage), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(null, OnSuccessMessageChanged));

    public static readonly DependencyProperty HasSuccessMessageProperty =
        DependencyProperty.Register(nameof(HasSuccessMessage), typeof(bool), typeof(ToolHeaderControl), new PropertyMetadata(false, OnHasSuccessMessageChanged));

    public static readonly DependencyProperty IsConfirmationOpenProperty =
        DependencyProperty.Register(nameof(IsConfirmationOpen), typeof(bool), typeof(ToolHeaderControl), new PropertyMetadata(false, OnIsConfirmationOpenChanged));

    public static readonly DependencyProperty ConfirmationTitleProperty =
        DependencyProperty.Register(nameof(ConfirmationTitle), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata("Confirm Action"));

    public static readonly DependencyProperty ConfirmationMessageProperty =
        DependencyProperty.Register(nameof(ConfirmationMessage), typeof(string), typeof(ToolHeaderControl), new PropertyMetadata(string.Empty));

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
        if (d is ToolHeaderControl control && e.NewValue is false)
        {
            if (!string.IsNullOrEmpty(control.ErrorMessage))
            {
                control.ErrorMessage = null;
            }
            if (control._subscribedVm is { HasError: true })
            {
                control._subscribedVm.HasError = false;
            }
        }
    }

    private static void OnSuccessMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolHeaderControl control)
        {
            bool hasSuccess = !string.IsNullOrEmpty(e.NewValue as string);
            if (control.HasSuccessMessage != hasSuccess)
            {
                control.HasSuccessMessage = hasSuccess;
            }
        }
    }

    private static void OnHasSuccessMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolHeaderControl control && e.NewValue is false)
        {
            if (!string.IsNullOrEmpty(control.SuccessMessage))
            {
                control.SuccessMessage = null;
            }
            if (control._subscribedVm is { HasSuccessMessage: true })
            {
                control._subscribedVm.HasSuccessMessage = false;
            }
        }
    }

    private static void OnIsConfirmationOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolHeaderControl control && e.NewValue is false)
        {
            if (control._subscribedVm is { IsConfirmationOpen: true })
            {
                control._subscribedVm.DismissConfirmation();
            }
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

    public string? SuccessMessage
    {
        get => (string?)GetValue(SuccessMessageProperty);
        set => SetValue(SuccessMessageProperty, value);
    }

    public bool HasSuccessMessage
    {
        get => (bool)GetValue(HasSuccessMessageProperty);
        set => SetValue(HasSuccessMessageProperty, value);
    }

    public bool IsConfirmationOpen
    {
        get => (bool)GetValue(IsConfirmationOpenProperty);
        set => SetValue(IsConfirmationOpenProperty, value);
    }

    public string ConfirmationTitle
    {
        get => (string)GetValue(ConfirmationTitleProperty);
        set => SetValue(ConfirmationTitleProperty, value);
    }

    public string ConfirmationMessage
    {
        get => (string)GetValue(ConfirmationMessageProperty);
        set => SetValue(ConfirmationMessageProperty, value);
    }

    public ToolHeaderControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }

        if (args.NewValue is ToolViewModelBase vm)
        {
            _subscribedVm = vm;
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
            SyncFromViewModel(vm);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ToolViewModelBase vm) return;

        DispatcherQueue?.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(ToolViewModelBase.SuccessMessage):
                case nameof(ToolViewModelBase.HasSuccessMessage):
                    SuccessMessage = vm.SuccessMessage;
                    break;
                case nameof(ToolViewModelBase.IsConfirmationOpen):
                case nameof(ToolViewModelBase.ConfirmationTitle):
                case nameof(ToolViewModelBase.ConfirmationMessage):
                    IsConfirmationOpen = vm.IsConfirmationOpen;
                    ConfirmationTitle = vm.ConfirmationTitle;
                    ConfirmationMessage = vm.ConfirmationMessage;
                    break;
            }
        });
    }

    private void SyncFromViewModel(ToolViewModelBase vm)
    {
        SuccessMessage = vm.SuccessMessage;
        IsConfirmationOpen = vm.IsConfirmationOpen;
        ConfirmationTitle = vm.ConfirmationTitle;
        ConfirmationMessage = vm.ConfirmationMessage;
    }

    private async void ConfirmActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_subscribedVm is not null)
        {
            await _subscribedVm.ConfirmPendingActionAsync();
        }
    }

    private void DismissConfirmationButton_Click(object sender, RoutedEventArgs e)
    {
        _subscribedVm?.DismissConfirmation();
    }
}
