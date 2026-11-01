using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.LuckyWheel;

namespace OmniForge.App.Modules.LuckyWheel;

/// <summary>
/// Production ViewModel governing Lucky Wheel configuration, item management,
/// physics spin calculation, audio tick events, and celebratory victory presentation.
/// </summary>
public partial class LuckyWheelViewModel : ToolViewModelBase
{
    private readonly ILuckyWheelService _wheelService;
    private readonly IClipboardService _clipboardService;

    private double _spinStartAngle;
    private double _spinTargetAngle;
    private double _spinElapsedTime;
    private double _previousFrameAngle;
    private SpinTarget? _activeTarget;

    public event Action<double>? WheelAngleUpdated;
    public event Action? DividerTickOccurred;
    public event Action<WheelItemViewModel>? SpinCompleted;

    public BulkObservableCollection<WheelItemViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial double CurrentAngle { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSpinning))]
    [NotifyCanExecuteChangedFor(nameof(StartSpinCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopSpinCommand))]
    public partial bool IsSpinning { get; set; }

    public bool IsNotSpinning => !IsSpinning;

    [ObservableProperty]
    public partial double SpinDurationSeconds { get; set; } = 4.5;

    [ObservableProperty]
    public partial DecelerationProfile DecelerationProfile { get; set; } = DecelerationProfile.EaseOutCubic;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WinningProbabilityText))]
    public partial WheelItemViewModel? WinningItem { get; set; }

    public string WinningProbabilityText => WinningItem is not null ? $"Probability: {WinningItem.Percentage:F1}%" : string.Empty;

    [ObservableProperty]
    public partial bool IsResultModalOpen { get; set; }

    [ObservableProperty]
    public partial string NewItemText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double NewItemWeight { get; set; } = 1.0;

    [ObservableProperty]
    public partial string ImportPayloadText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsImportDialogOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveItemsText))]
    public partial int ActiveItemsCount { get; set; }

    public string ActiveItemsText => $"Active: {ActiveItemsCount}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalWeightText))]
    public partial double TotalWeight { get; set; }

    public string TotalWeightText => $"Weight: {TotalWeight:F1}";

    public bool IsOptionsEmpty => Items.Count == 0;
    public bool HasOptions => Items.Count > 0;

    [ObservableProperty]
    public partial string SelectedPreset { get; set; } = "Decision Maker";

    public IReadOnlyList<string> AvailablePresets => _wheelService.GetAvailablePresets();

    public IReadOnlyList<WheelSlice> Slices { get; private set; } = [];

    public LuckyWheelViewModel(ILuckyWheelService wheelService, IClipboardService clipboardService)
    {
        _wheelService = wheelService ?? throw new ArgumentNullException(nameof(wheelService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));

        Items.CollectionChanged += OnItemsCollectionChanged;

        // Load initial default preset
        LoadPreset("Decision Maker");
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (WheelItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (WheelItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        RecalculateSlices();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WheelItemViewModel.Weight) or
            nameof(WheelItemViewModel.IsEnabled) or
            nameof(WheelItemViewModel.Text) or
            nameof(WheelItemViewModel.ColorHex))
        {
            RecalculateSlices();
        }
    }

    public void RecalculateSlices()
    {
        var models = Items.Select(vm => vm.ToModel()).ToList();
        Slices = LuckyWheelEngine.CalculateSlices(models);

        ActiveItemsCount = Slices.Count;
        TotalWeight = Slices.Sum(s => s.Item.Weight);

        // Update percentage metadata on view models
        foreach (var vm in Items)
        {
            var match = Slices.FirstOrDefault(s => s.Item.Id == vm.Id);
            vm.Percentage = match?.Percentage ?? 0.0;
        }

        StartSpinCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsOptionsEmpty));
        OnPropertyChanged(nameof(HasOptions));
        WheelAngleUpdated?.Invoke(CurrentAngle);
    }

    public bool CanSpin => !IsSpinning && ActiveItemsCount >= 2;

    [RelayCommand(CanExecute = nameof(CanSpin))]
    public void StartSpin()
    {
        if (!CanSpin) return;

        ClearError();
        IsResultModalOpen = false;
        WinningItem = null;

        try
        {
            _activeTarget = LuckyWheelEngine.CalculateTarget(
                Slices,
                CurrentAngle,
                minRotations: 5,
                maxRotations: 8,
                durationSeconds: Math.Clamp(SpinDurationSeconds, 2.0, 15.0));

            _spinStartAngle = CurrentAngle;
            _spinTargetAngle = _activeTarget.TargetAngle;
            _spinElapsedTime = 0.0;
            _previousFrameAngle = CurrentAngle;

            IsSpinning = true;
            IsBusy = true;
            StatusMessage = "Wheel is spinning...";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to start spin: {ex.Message}";
            IsSpinning = false;
            IsBusy = false;
        }
    }

    /// <summary>
    /// Frame-by-frame physics stepping invoked by the 60/120 FPS CompositionTarget.Rendering loop.
    /// </summary>
    public void AdvancePhysicsStep(double deltaSeconds)
    {
        if (!IsSpinning || _activeTarget is null) return;

        _spinElapsedTime += deltaSeconds;
        double progress = Math.Clamp(_spinElapsedTime / _activeTarget.DurationSeconds, 0.0, 1.0);
        ReportProgress(progress * 100.0, $"Wheel is spinning... ({progress * 100.0:F0}%)");

        double evaluatedAngle = LuckyWheelEngine.EvaluateAngle(
            _spinStartAngle,
            _spinTargetAngle,
            progress,
            DecelerationProfile);

        // Detect ticker collision with dividers
        int crossings = LuckyWheelEngine.DetectDividerCrossings(_previousFrameAngle, evaluatedAngle, Slices);
        if (crossings > 0)
        {
            for (int i = 0; i < crossings; i++)
            {
                DividerTickOccurred?.Invoke();
            }
        }

        CurrentAngle = evaluatedAngle;
        _previousFrameAngle = evaluatedAngle;
        WheelAngleUpdated?.Invoke(CurrentAngle);

        if (progress >= 1.0)
        {
            CompleteSpin();
        }
    }

    [RelayCommand(CanExecute = nameof(IsSpinning))]
    public void StopSpin()
    {
        if (!IsSpinning) return;
        IsSpinning = false;
        IsBusy = false;
        StatusMessage = "Wheel spin cancelled by user.";
    }

    private void CompleteSpin()
    {
        IsSpinning = false;
        IsBusy = false;
        StatusMessage = "Spin complete!";

        if (_activeTarget is not null)
        {
            var winnerVm = Items.FirstOrDefault(vm => vm.Id == _activeTarget.WinningItem.Id);
            if (winnerVm is not null)
            {
                WinningItem = winnerVm;
                IsResultModalOpen = true;
                SpinCompleted?.Invoke(winnerVm);
            }
        }
    }

    [RelayCommand]
    public void AddNewItem()
    {
        if (string.IsNullOrWhiteSpace(NewItemText))
        {
            NewItemText = $"Option {Items.Count + 1}";
        }

        int colorIdx = Items.Count % _wheelService.PaletteColors.Count;
        string color = _wheelService.PaletteColors[colorIdx];

        var newItem = new WheelItemViewModel
        {
            Text = NewItemText.Trim(),
            Weight = Math.Max(0.1, NewItemWeight),
            ColorHex = color,
            IsEnabled = true
        };

        Items.Add(newItem);
        NewItemText = string.Empty;
        NewItemWeight = 1.0;
    }

    [RelayCommand]
    public void DeleteItem(WheelItemViewModel? item)
    {
        if (item is null) return;
        Items.Remove(item);
    }

    [RelayCommand]
    public void ClearItems()
    {
        if (IsSpinning || Items.Count == 0) return;

        RequestConfirmation(
            title: "Confirm Clear All Wheel Options",
            message: $"Remove all {Items.Count} option(s) from the wheel?",
            onConfirmedAction: _ =>
            {
                Items.Clear();
                StatusMessage = "Cleared all wheel options.";
                SuccessMessage = "Cleared all wheel options.";
                return Task.CompletedTask;
            });
    }

    [RelayCommand]
    public void EqualizeWeights()
    {
        if (IsSpinning || Items.Count == 0) return;
        foreach (var item in Items)
        {
            item.Weight = 1.0;
        }
        RecalculateSlices();
        StatusMessage = "Reset all option weights to equal (1.0).";
    }

    [RelayCommand]
    public void ShuffleItems()
    {
        if (Items.Count <= 1 || IsSpinning) return;

        var shuffled = _wheelService.ShuffleItems(Items.Select(vm => vm.ToModel()));
        Items.Clear();
        foreach (var item in shuffled)
        {
            Items.Add(new WheelItemViewModel(item));
        }
    }

    [RelayCommand]
    public void LoadPreset(string? presetName)
    {
        if (IsSpinning) return;
        string target = string.IsNullOrWhiteSpace(presetName) ? SelectedPreset : presetName;
        SelectedPreset = target;

        var presetItems = _wheelService.GetPreset(target);
        Items.Clear();
        foreach (var item in presetItems)
        {
            Items.Add(new WheelItemViewModel(item));
        }
    }

    [RelayCommand]
    public async Task ImportFromClipboardAsync()
    {
        if (IsSpinning) return;

        var result = await _clipboardService.GetTextAsync();
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
        {
            ImportPayloadText = result.Value;
            ExecuteImport();
        }
        else
        {
            ErrorMessage = "Clipboard contains no text data.";
        }
    }

    [RelayCommand]
    public void ExecuteImport()
    {
        if (IsSpinning) return;

        var parseResult = _wheelService.ParseItemsFromText(ImportPayloadText);
        if (parseResult.IsSuccess && parseResult.Value is { Count: > 0 } parsedList)
        {
            Items.Clear();
            foreach (var item in parsedList)
            {
                Items.Add(new WheelItemViewModel(item));
            }

            IsImportDialogOpen = false;
            ImportPayloadText = string.Empty;
            StatusMessage = $"Imported {parsedList.Count} items.";
        }
        else
        {
            ErrorMessage = parseResult.Error.Message;
        }
    }

    [RelayCommand]
    public void RemoveWinner()
    {
        if (WinningItem is not null)
        {
            Items.Remove(WinningItem);
            WinningItem = null;
            IsResultModalOpen = false;
        }
    }

    [RelayCommand]
    public void CloseResultModal()
    {
        IsResultModalOpen = false;
    }

    [RelayCommand]
    public void CopyWinnerToClipboard()
    {
        if (WinningItem is not null)
        {
            _clipboardService.CopyText(WinningItem.Text);
            StatusMessage = $"Copied \"{WinningItem.Text}\" to clipboard.";
        }
    }
}
