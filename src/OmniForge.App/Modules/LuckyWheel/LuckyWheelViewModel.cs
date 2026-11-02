using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.LuckyWheel;

namespace OmniForge.App.Modules.LuckyWheel;

/// <summary>
/// Production ViewModel governing Lucky Wheel configuration, item management,
/// physics spin calculation, smart Excel/TXT/CSV importing, audio tick events,
/// winner history, and celebratory victory presentation.
/// </summary>
public partial class LuckyWheelViewModel : ToolViewModelBase
{
    private readonly ILuckyWheelService _wheelService;
    private readonly IClipboardService _clipboardService;
    private readonly IFilePickerService _filePickerService;
    private readonly ILocalizationService? _localizationService;

    private double _spinStartAngle;
    private double _spinTargetAngle;
    private double _spinElapsedTime;
    private double _previousFrameAngle;
    private SpinTarget? _activeTarget;
    private List<WheelItem> _cachedParsedFileItems = [];

    public event Action<double>? WheelAngleUpdated;
    public event Action? DividerTickOccurred;
    public event Action<WheelItemViewModel>? SpinCompleted;

    public BulkObservableCollection<WheelItemViewModel> Items { get; } = [];
    public BulkObservableCollection<WheelItemViewModel> FilteredItems { get; } = [];
    public BulkObservableCollection<WinnerHistoryEntryViewModel> History { get; } = [];
    public BulkObservableCollection<WheelItemViewModel> ImportPreviewItems { get; } = [];

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

    [ObservableProperty]
    public partial string WinningTimestampText { get; set; } = string.Empty;

    private bool IsVietnamese => _localizationService?.CurrentLanguage == AppLanguage.Vietnamese;

    public string EmptyWheelText => _localizationService?.GetString(LocalizationKeys.LuckyWheel_Empty_Title)
        ?? (IsVietnamese ? "Thêm đối tượng để quay" : "Add items to spin");

    public string CenterHubText => IsVietnamese ? "QUAY" : "SPIN";

    public string WinningProbabilityText => WinningItem is not null
        ? (IsVietnamese
            ? $"Tỉ lệ trúng: {WinningItem.Percentage.ToString("F1", CultureInfo.InvariantCulture)}%"
            : $"Winning odds: {WinningItem.Percentage.ToString("F1", CultureInfo.InvariantCulture)}%")
        : string.Empty;

    [ObservableProperty]
    public partial bool IsResultModalOpen { get; set; }

    [ObservableProperty]
    public partial string NewItemText { get; set; } = string.Empty;

    private double _newItemWeight = 1.0;

    public double NewItemWeight
    {
        get => _newItemWeight;
        set
        {
            double sanitized = double.IsNaN(value) || double.IsInfinity(value)
                ? 1.0
                : Math.Round(Math.Clamp(value, 0.0, 10.0), 2);

            if (SetProperty(ref _newItemWeight, sanitized))
            {
                OnPropertyChanged(nameof(NewItemWeightText));
            }
        }
    }

    public string NewItemWeightText
    {
        get => NewItemWeight.ToString("0.##", CultureInfo.InvariantCulture);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                OnPropertyChanged(nameof(NewItemWeightText));
                return;
            }

            string normalized = value.Trim().TrimEnd('%').Trim().Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                NewItemWeight = parsed;
            }
            else
            {
                OnPropertyChanged(nameof(NewItemWeightText));
            }
        }
    }

    [RelayCommand]
    public void IncreaseNewItemWeight()
    {
        NewItemWeight = Math.Round(Math.Min(10.0, NewItemWeight + 0.5), 2);
    }

    [RelayCommand]
    public void DecreaseNewItemWeight()
    {
        NewItemWeight = Math.Round(Math.Max(0.0, NewItemWeight - 0.5), 2);
    }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    // Smart Import Modal Properties
    [ObservableProperty]
    public partial bool IsImportModalOpen { get; set; }

    [ObservableProperty]
    public partial int ImportTabIndex { get; set; }

    [ObservableProperty]
    public partial bool IsReplaceMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAppendMode { get; set; }

    [ObservableProperty]
    public partial string SelectedImportFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImportFileStatusText { get; set; } = "Chưa chọn file nào. Hỗ trợ .xlsx, .csv, .tsv, .txt";

    [ObservableProperty]
    public partial string ImportPayloadText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasImportPreview { get; set; }

    [ObservableProperty]
    public partial string ImportPreviewSummary { get; set; } = string.Empty;

    // Status & Count Summaries
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveItemsText))]
    public partial int ActiveItemsCount { get; set; }

    public string ActiveItemsText =>
        _localizationService?.GetFormattedString(LocalizationKeys.LuckyWheel_Badge_Active, ActiveItemsCount)
        ?? $"Đang bật: {ActiveItemsCount}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalWeightText))]
    public partial double TotalWeight { get; set; }

    public string TotalWeightText =>
        _localizationService?.GetFormattedString(LocalizationKeys.LuckyWheel_Badge_Weight, TotalWeight.ToString("F1", CultureInfo.InvariantCulture))
        ?? $"Trọng số: {TotalWeight.ToString("F1", CultureInfo.InvariantCulture)}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsCountSummaryText))]
    public partial int TotalItemsCount { get; set; }

    public string ItemsCountSummaryText =>
        _localizationService?.GetFormattedString(LocalizationKeys.LuckyWheel_Badge_Count, FilteredItems.Count, Items.Count)
        ?? $"{FilteredItems.Count} / {Items.Count} mục";

    public bool IsOptionsEmpty => Items.Count == 0;
    public bool HasOptions => Items.Count > 0;

    // Winner History & Drawer
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHistory))]
    [NotifyPropertyChangedFor(nameof(HistoryButtonText))]
    public partial bool IsHistoryDrawerOpen { get; set; }

    public bool HasHistory => History.Count > 0;
    public string HistoryButtonText =>
        _localizationService?.GetFormattedString(LocalizationKeys.LuckyWheel_HistoryBtn, History.Count)
        ?? $"Lịch sử ({History.Count})";

    // Audio & Effects Settings
    [ObservableProperty]
    public partial bool IsSoundEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsConfettiEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string SelectedPreset { get; set; } = "Decision Maker";

    public IReadOnlyList<string> AvailablePresets => _wheelService.GetAvailablePresets();

    public IReadOnlyList<WheelSlice> Slices { get; private set; } = [];

    public LuckyWheelViewModel(
        ILuckyWheelService wheelService,
        IClipboardService clipboardService,
        IFilePickerService filePickerService,
        ILocalizationService? localizationService = null)
    {
        _wheelService = wheelService ?? throw new ArgumentNullException(nameof(wheelService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
        _localizationService = localizationService;

        if (_localizationService is not null)
        {
            _localizationService.LanguageChanged += OnLanguageChanged;
            ImportFileStatusText = _localizationService.GetString(LocalizationKeys.LuckyWheel_Import_NoFile);
        }

        Items.CollectionChanged += OnItemsCollectionChanged;

        // Load initial default preset
        LoadPreset("Decision Maker");
    }

    private void OnLanguageChanged(object? sender, AppLanguage newLanguage)
    {
        OnPropertyChanged(nameof(ActiveItemsText));
        OnPropertyChanged(nameof(TotalWeightText));
        OnPropertyChanged(nameof(ItemsCountSummaryText));
        OnPropertyChanged(nameof(HistoryButtonText));
        OnPropertyChanged(nameof(WinningProbabilityText));
        OnPropertyChanged(nameof(EmptyWheelText));
        OnPropertyChanged(nameof(CenterHubText));
        if (string.IsNullOrEmpty(SelectedImportFilePath))
        {
            ImportFileStatusText = _localizationService?.GetString(LocalizationKeys.LuckyWheel_Import_NoFile) ?? ImportFileStatusText;
        }
        WheelAngleUpdated?.Invoke(CurrentAngle);
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

        TotalItemsCount = Items.Count;
        ApplyFilter();
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

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            FilteredItems.Clear();
            FilteredItems.AddRange(Items);
        }
        else
        {
            string query = SearchQuery.Trim();
            var matched = Items.Where(i => i.Text.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            FilteredItems.Clear();
            FilteredItems.AddRange(matched);
        }

        OnPropertyChanged(nameof(ItemsCountSummaryText));
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
        OnPropertyChanged(nameof(ItemsCountSummaryText));
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
            StatusMessage = "Vòng quay đang quay...";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể khởi động vòng quay: {ex.Message}";
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
        ReportProgress(progress * 100.0, $"Vòng quay đang quay... ({progress * 100.0:F0}%)");

        double evaluatedAngle = LuckyWheelEngine.EvaluateAngle(
            _spinStartAngle,
            _spinTargetAngle,
            progress,
            DecelerationProfile);

        // Detect ticker collision with dividers
        if (IsSoundEnabled)
        {
            int crossings = LuckyWheelEngine.DetectDividerCrossings(_previousFrameAngle, evaluatedAngle, Slices);
            if (crossings > 0)
            {
                for (int i = 0; i < crossings; i++)
                {
                    DividerTickOccurred?.Invoke();
                }
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
        StatusMessage = "Vòng quay đã được dừng.";
    }

    private void CompleteSpin()
    {
        IsSpinning = false;
        IsBusy = false;
        StatusMessage = "Quay hoàn tất!";

        if (_activeTarget is not null)
        {
            var winnerVm = Items.FirstOrDefault(vm => vm.Id == _activeTarget.WinningItem.Id);
            if (winnerVm is not null)
            {
                WinningItem = winnerVm;
                WinningTimestampText = $"Thời gian: {DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}";
                IsResultModalOpen = true;

                // Record into history
                var historyItem = new WinnerHistoryEntryViewModel(
                    winnerVm.Text,
                    winnerVm.Weight,
                    winnerVm.Percentage,
                    winnerVm.ColorHex,
                    DateTime.Now);

                History.Insert(0, historyItem);
                OnPropertyChanged(nameof(HasHistory));
                OnPropertyChanged(nameof(HistoryButtonText));

                SpinCompleted?.Invoke(winnerVm);
            }
        }
    }

    /// <summary>
    /// Smart quick-add supporting single or multi-item entry separated by commas or newlines.
    /// </summary>
    [RelayCommand]
    public void AddNewItem()
    {
        if (string.IsNullOrWhiteSpace(NewItemText))
        {
            NewItemText = $"Mục {Items.Count + 1}";
        }

        string raw = NewItemText.Trim();
        char[] separators = [',', ';', '\r', '\n'];
        var tokens = raw.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length > 1)
        {
            var toAdd = new List<WheelItemViewModel>(tokens.Length);
            int baseColorIdx = Items.Count;

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;
                string color = _wheelService.PaletteColors[baseColorIdx % _wheelService.PaletteColors.Count];
                baseColorIdx++;

                toAdd.Add(new WheelItemViewModel
                {
                    Text = token,
                    Weight = Math.Max(0.1, NewItemWeight),
                    ColorHex = color,
                    IsEnabled = true
                });
            }

            Items.AddRange(toAdd);
            StatusMessage = $"Đã thêm nhanh {toAdd.Count} đối tượng.";
        }
        else
        {
            int colorIdx = Items.Count % _wheelService.PaletteColors.Count;
            string color = _wheelService.PaletteColors[colorIdx];

            var newItem = new WheelItemViewModel
            {
                Text = raw,
                Weight = Math.Max(0.1, NewItemWeight),
                ColorHex = color,
                IsEnabled = true
            };

            Items.Add(newItem);
            StatusMessage = $"Đã thêm: \"{raw}\".";
        }

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
            title: "Xác nhận xóa danh sách",
            message: $"Bạn có chắc muốn xóa toàn bộ {Items.Count} đối tượng trên vòng quay?",
            onConfirmedAction: _ =>
            {
                Items.Clear();
                StatusMessage = "Đã xóa toàn bộ đối tượng.";
                SuccessMessage = "Đã làm trống vòng quay.";
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
        StatusMessage = "Đã đặt lại toàn bộ trọng số về 1.0 (tỉ lệ đều nhau).";
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
        StatusMessage = "Đã xáo trộn ngẫu nhiên thứ tự các ô.";
    }

    [RelayCommand]
    public void SortByName()
    {
        if (Items.Count <= 1 || IsSpinning) return;
        var sorted = Items.OrderBy(i => i.Text, StringComparer.CurrentCultureIgnoreCase).ToList();
        Items.Clear();
        Items.AddRange(sorted);
        StatusMessage = "Đã sắp xếp danh sách theo bảng chữ cái A-Z.";
    }

    [RelayCommand]
    public void SortByWeight()
    {
        if (Items.Count <= 1 || IsSpinning) return;
        var sorted = Items.OrderByDescending(i => i.Weight).ToList();
        Items.Clear();
        Items.AddRange(sorted);
        StatusMessage = "Đã sắp xếp danh sách theo trọng số giảm dần.";
    }

    [RelayCommand]
    public void EnableAllItems()
    {
        if (IsSpinning || Items.Count == 0) return;
        foreach (var item in Items)
        {
            item.IsEnabled = true;
        }
        RecalculateSlices();
        StatusMessage = "Đã bật tất cả đối tượng.";
    }

    [RelayCommand]
    public void DisableAllItems()
    {
        if (IsSpinning || Items.Count == 0) return;
        foreach (var item in Items)
        {
            item.IsEnabled = false;
        }
        RecalculateSlices();
        StatusMessage = "Đã tắt tất cả đối tượng.";
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
        StatusMessage = $"Đã tải mẫu: {target}.";
    }

    // =========================================================================
    // SMART IMPORT MODAL & FILE PICKING
    // =========================================================================

    [RelayCommand]
    public void OpenImportModal()
    {
        if (IsSpinning) return;
        SelectedImportFilePath = string.Empty;
        ImportFileStatusText = "Chưa chọn file nào. Hỗ trợ .xlsx, .csv, .tsv, .txt";
        ImportPayloadText = string.Empty;
        HasImportPreview = false;
        ImportPreviewSummary = string.Empty;
        ImportPreviewItems.Clear();
        _cachedParsedFileItems.Clear();
        IsImportModalOpen = true;
    }

    [RelayCommand]
    public void CloseImportModal()
    {
        IsImportModalOpen = false;
    }

    [RelayCommand]
    public async Task PickImportFileAsync()
    {
        if (IsSpinning) return;

        ClearError();
        var pickResult = await _filePickerService.PickSingleFileAsync(
            [".xlsx", ".xls", ".csv", ".tsv", ".txt"]);

        if (!pickResult.IsSuccess || string.IsNullOrWhiteSpace(pickResult.Value))
        {
            return;
        }

        string filePath = pickResult.Value;
        SelectedImportFilePath = filePath;
        ImportFileStatusText = $"Đang đọc file: {Path.GetFileName(filePath)}...";

        var parseResult = await _wheelService.ImportFromFileAsync(filePath);
        if (parseResult.IsSuccess && parseResult.Value is { Count: > 0 } parsedItems)
        {
            _cachedParsedFileItems = [.. parsedItems];
            ImportPreviewItems.Clear();

            // Show preview of first 8 items
            foreach (var item in parsedItems.Take(8))
            {
                ImportPreviewItems.Add(new WheelItemViewModel(item));
            }

            HasImportPreview = true;
            ImportPreviewSummary = $"Tìm thấy {parsedItems.Count} đối tượng hợp lệ trong file (xem trước {ImportPreviewItems.Count} mục đầu).";
            ImportFileStatusText = $"Đã đọc thành công: {Path.GetFileName(filePath)} ({parsedItems.Count} mục)";
        }
        else
        {
            HasImportPreview = false;
            _cachedParsedFileItems.Clear();
            ImportPreviewItems.Clear();
            ImportFileStatusText = $"Lỗi đọc file: {parseResult.Error.Message}";
            ErrorMessage = parseResult.Error.Message;
        }
    }

    [RelayCommand]
    public void ExecuteImport()
    {
        if (IsSpinning) return;

        IReadOnlyList<WheelItem>? itemsToImport = null;

        // If user is on File tab (tab 0) and has cached parsed items from picked file
        if (ImportTabIndex == 0 && _cachedParsedFileItems.Count > 0)
        {
            itemsToImport = _cachedParsedFileItems;
        }
        else
        {
            // Raw text tab (tab 1)
            var textResult = _wheelService.ParseItemsFromText(ImportPayloadText);
            if (textResult.IsSuccess && textResult.Value is { Count: > 0 } parsedList)
            {
                itemsToImport = parsedList;
            }
            else
            {
                ErrorMessage = textResult.Error.Message;
                return;
            }
        }

        if (itemsToImport is null || itemsToImport.Count == 0)
        {
            ErrorMessage = "Không tìm thấy dữ liệu đối tượng nào hợp lệ để nhập.";
            return;
        }

        if (IsReplaceMode)
        {
            Items.Clear();
        }

        var newVms = itemsToImport.Select(item => new WheelItemViewModel(item)).ToList();
        Items.AddRange(newVms);

        IsImportModalOpen = false;
        ImportPayloadText = string.Empty;
        _cachedParsedFileItems.Clear();
        StatusMessage = $"Đã nhập thành công {itemsToImport.Count} đối tượng vào vòng quay.";
        SuccessMessage = $"Đã nhập {itemsToImport.Count} mục!";
    }

    [RelayCommand]
    public async Task ImportFromClipboardAsync()
    {
        if (IsSpinning) return;

        var result = await _clipboardService.GetTextAsync();
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
        {
            ImportPayloadText = result.Value;
            ImportTabIndex = 1;
            IsImportModalOpen = true;
        }
        else
        {
            ErrorMessage = "Bộ nhớ tạm (Clipboard) không chứa dữ liệu văn bản.";
        }
    }

    [RelayCommand]
    public void ExportToClipboard()
    {
        if (Items.Count == 0) return;
        string csv = _wheelService.ExportItemsToCsv(Items.Select(vm => vm.ToModel()));
        _clipboardService.CopyText(csv);
        StatusMessage = $"Đã sao chép danh sách {Items.Count} đối tượng dạng CSV vào Clipboard.";
        SuccessMessage = "Đã chép danh sách CSV!";
    }

    // =========================================================================
    // WINNER MODAL & ACTIONS
    // =========================================================================

    [RelayCommand]
    public void SpinAgain()
    {
        IsResultModalOpen = false;
        StartSpin();
    }

    [RelayCommand]
    public void RemoveWinnerAndSpin()
    {
        if (WinningItem is not null)
        {
            Items.Remove(WinningItem);
            WinningItem = null;
        }
        IsResultModalOpen = false;
        StartSpin();
    }

    [RelayCommand]
    public void RemoveWinner()
    {
        if (WinningItem is not null)
        {
            Items.Remove(WinningItem);
            WinningItem = null;
            IsResultModalOpen = false;
            StatusMessage = "Đã loại người trúng khỏi vòng quay.";
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
            StatusMessage = $"Đã sao chép: \"{WinningItem.Text}\" vào Clipboard.";
        }
    }

    // =========================================================================
    // WINNER HISTORY
    // =========================================================================

    [RelayCommand]
    public void ToggleHistory()
    {
        IsHistoryDrawerOpen = !IsHistoryDrawerOpen;
    }

    [RelayCommand]
    public void ClearHistory()
    {
        History.Clear();
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HistoryButtonText));
        StatusMessage = "Đã xóa toàn bộ lịch sử trúng thưởng.";
    }

    [RelayCommand]
    public void CopyHistory()
    {
        if (History.Count == 0) return;
        var lines = History.Select(h => $"{h.FormattedTime}: {h.Name} ({h.FormattedOdds})");
        string text = string.Join(Environment.NewLine, lines);
        _clipboardService.CopyText(text);
        StatusMessage = "Đã sao chép toàn bộ lịch sử vào Clipboard.";
    }
}
