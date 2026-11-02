using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OmniForge.App.Helpers;
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
            string? newMsg = e.NewValue as string;
            bool hasSuccess = !string.IsNullOrEmpty(newMsg);
            if (control.HasSuccessMessage != hasSuccess)
            {
                control.HasSuccessMessage = hasSuccess;
            }

            if (hasSuccess)
            {
                control.StartSuccessDismissTimer();
            }
            else
            {
                control._successDismissTimer?.Stop();
            }
        }
    }

    private static void OnHasSuccessMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolHeaderControl control && e.NewValue is false)
        {
            control._successDismissTimer?.Stop();
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
        if (d is ToolHeaderControl control)
        {
            if (e.NewValue is true)
            {
                if (control.IsLoaded)
                {
                    _ = control.ShowConfirmationDialogAsync();
                }
                else
                {
                    RoutedEventHandler? loadedHandler = null;
                    loadedHandler = (s, args) =>
                    {
                        control.Loaded -= loadedHandler;
                        if (control.IsConfirmationOpen)
                        {
                            _ = control.ShowConfirmationDialogAsync();
                        }
                    };
                    control.Loaded += loadedHandler;
                }
            }
            else if (e.NewValue is false)
            {
                control._activeConfirmDialog?.Hide();
                if (control._subscribedVm is { IsConfirmationOpen: true })
                {
                    control._subscribedVm.DismissConfirmation();
                }
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
        _successDismissTimer?.Stop();
        _activeConfirmDialog?.Hide();
        _activeConfirmDialog = null;
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
                case nameof(ToolViewModelBase.ConfirmationTitle):
                    ConfirmationTitle = vm.ConfirmationTitle;
                    break;
                case nameof(ToolViewModelBase.ConfirmationMessage):
                    ConfirmationMessage = vm.ConfirmationMessage;
                    break;
                case nameof(ToolViewModelBase.IsConfirmationOpen):
                    ConfirmationTitle = vm.ConfirmationTitle;
                    ConfirmationMessage = vm.ConfirmationMessage;
                    IsConfirmationOpen = vm.IsConfirmationOpen;
                    break;
            }
        });
    }

    private void SyncFromViewModel(ToolViewModelBase vm)
    {
        SuccessMessage = vm.SuccessMessage;
        ConfirmationTitle = vm.ConfirmationTitle;
        ConfirmationMessage = vm.ConfirmationMessage;
        IsConfirmationOpen = vm.IsConfirmationOpen;
    }

    private DispatcherTimer? _successDismissTimer;
    private ContentDialog? _activeConfirmDialog;

    private void StartSuccessDismissTimer()
    {
        _successDismissTimer?.Stop();
        if (_successDismissTimer is null)
        {
            _successDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _successDismissTimer.Tick += (s, e) =>
            {
                _successDismissTimer?.Stop();
                HasSuccessMessage = false;
                SuccessMessage = null;
                if (_subscribedVm is not null)
                {
                    _subscribedVm.HasSuccessMessage = false;
                    _subscribedVm.SuccessMessage = null;
                }
            };
        }
        _successDismissTimer.Start();
    }

    private async Task ShowConfirmationDialogAsync()
    {
        if (_activeConfirmDialog is not null) return;

        var xamlRoot = XamlRoot ?? (Content as FrameworkElement)?.XamlRoot;
        if (xamlRoot is null) return;

        var loc = LocalizationManager.Current;
        string confirmText = loc?["Control_ToolHeader_ConfirmAndExecute"] ?? "Confirm & Execute";
        string cancelText = loc?["Control_ToolHeader_Cancel"] ?? "Cancel";

        // Build rich confirmation content with visual warning badge
        var badgeBorder = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(21),
            Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var warningIcon = new FontIcon
        {
            Glyph = "\uE7BA", // Warning badge
            FontSize = 20,
            Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        badgeBorder.Child = warningIcon;

        string title = !string.IsNullOrWhiteSpace(_subscribedVm?.ConfirmationTitle)
            ? _subscribedVm.ConfirmationTitle
            : (!string.IsNullOrWhiteSpace(ConfirmationTitle) ? ConfirmationTitle : "Confirm Action");

        string message = !string.IsNullOrWhiteSpace(_subscribedVm?.ConfirmationMessage)
            ? _subscribedVm.ConfirmationMessage
            : ConfirmationMessage;

        var titleText = new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        var headerStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children = { badgeBorder, titleText }
        };

        var container = new StackPanel
        {
            Spacing = 16,
            MaxWidth = 420,
            Children = { headerStack }
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            var messageBorder = new Border
            {
                Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12)
            };

            var messageText = new TextBlock
            {
                Text = message,
                Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                TextWrapping = TextWrapping.Wrap
            };
            messageBorder.Child = messageText;
            container.Children.Add(messageBorder);
        }

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            RequestedTheme = ActualTheme,
            Content = container,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Primary
        };

        _activeConfirmDialog = dialog;

        try
        {
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (_subscribedVm is not null)
                {
                    await _subscribedVm.ConfirmPendingActionAsync();
                }
            }
            else
            {
                _subscribedVm?.DismissConfirmation();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _subscribedVm?.DismissConfirmation();
        }
        finally
        {
            _activeConfirmDialog = null;
            IsConfirmationOpen = false;
        }
    }
}
