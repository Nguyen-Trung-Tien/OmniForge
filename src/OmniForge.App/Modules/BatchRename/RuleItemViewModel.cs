using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Contracts;
using OmniForge.Modules.BatchOps;

namespace OmniForge.App.Modules.BatchRename;

public enum RenameRuleType
{
    PrefixSuffix,
    Replace,
    ChangeCase,
    Numbering
}

/// <summary>
/// Observable ViewModel representing an editable transformation rule card.
/// Automatically notifies the parent view model to trigger reactive preview debouncing.
/// </summary>
public partial class RuleItemViewModel : ObservableObject
{
    private readonly Action? _onRuleChanged;
    private readonly Action<RuleItemViewModel>? _onRemove;
    private readonly Action<RuleItemViewModel>? _onMoveUp;
    private readonly Action<RuleItemViewModel>? _onMoveDown;

    public RenameRuleType RuleType { get; }

    [ObservableProperty]
    public partial bool IsActive { get; set; } = true;

    // --- Prefix / Suffix ---
    [ObservableProperty]
    public partial string Prefix { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Suffix { get; set; } = string.Empty;

    // --- Search & Replace ---
    [ObservableProperty]
    public partial string SearchPattern { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Replacement { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool MatchCase { get; set; }

    [ObservableProperty]
    public partial bool UseRegex { get; set; }

    // --- Change Case ---
    [ObservableProperty]
    public partial int SelectedCaseIndex { get; set; } // 0 = Lowercase, 1 = Uppercase, 2 = Title Case

    // --- Numbering ---
    [ObservableProperty]
    public partial int StartNumber { get; set; } = 1;

    [ObservableProperty]
    public partial int Step { get; set; } = 1;

    [ObservableProperty]
    public partial string PaddingFormat { get; set; } = "001";

    [ObservableProperty]
    public partial int SelectedPositionIndex { get; set; } // 0 = Suffix, 1 = Prefix

    [ObservableProperty]
    public partial string Separator { get; set; } = "_";

    // View helper flags
    public bool IsPrefixSuffix => RuleType == RenameRuleType.PrefixSuffix;
    public bool IsReplace => RuleType == RenameRuleType.Replace;
    public bool IsChangeCase => RuleType == RenameRuleType.ChangeCase;
    public bool IsNumbering => RuleType == RenameRuleType.Numbering;

    public string Title => RuleType switch
    {
        RenameRuleType.PrefixSuffix => "Prefix / Suffix",
        RenameRuleType.Replace => "Search & Replace",
        RenameRuleType.ChangeCase => "Change Case",
        RenameRuleType.Numbering => "Numbering",
        _ => "Rule"
    };

    public string IconGlyph => RuleType switch
    {
        RenameRuleType.PrefixSuffix => "\uE8D3",
        RenameRuleType.Replace => "\uE721",
        RenameRuleType.ChangeCase => "\uE8D2",
        RenameRuleType.Numbering => "\uE8EC",
        _ => "\uE8AC"
    };

    public RuleItemViewModel(
        RenameRuleType type,
        Action? onRuleChanged = null,
        Action<RuleItemViewModel>? onRemove = null,
        Action<RuleItemViewModel>? onMoveUp = null,
        Action<RuleItemViewModel>? onMoveDown = null)
    {
        RuleType = type;
        _onRuleChanged = onRuleChanged;
        _onRemove = onRemove;
        _onMoveUp = onMoveUp;
        _onMoveDown = onMoveDown;
    }

    [RelayCommand]
    private void Remove() => _onRemove?.Invoke(this);

    [RelayCommand]
    private void MoveUp() => _onMoveUp?.Invoke(this);

    [RelayCommand]
    private void MoveDown() => _onMoveDown?.Invoke(this);

    partial void OnIsActiveChanged(bool value) => NotifyChange();
    partial void OnPrefixChanged(string value) => NotifyChange();
    partial void OnSuffixChanged(string value) => NotifyChange();
    partial void OnSearchPatternChanged(string value) => NotifyChange();
    partial void OnReplacementChanged(string value) => NotifyChange();
    partial void OnMatchCaseChanged(bool value) => NotifyChange();
    partial void OnUseRegexChanged(bool value) => NotifyChange();
    partial void OnSelectedCaseIndexChanged(int value) => NotifyChange();
    partial void OnStartNumberChanged(int value) => NotifyChange();
    partial void OnStepChanged(int value) => NotifyChange();
    partial void OnPaddingFormatChanged(string value) => NotifyChange();
    partial void OnSelectedPositionIndexChanged(int value) => NotifyChange();
    partial void OnSeparatorChanged(string value) => NotifyChange();

    private void NotifyChange() => _onRuleChanged?.Invoke();

    /// <summary>
    /// Translates this ViewModel into an immutable <see cref="IRenameRule"/> instance for execution.
    /// </summary>
    public IRenameRule ToRule()
    {
        return RuleType switch
        {
            RenameRuleType.PrefixSuffix => new PrefixSuffixRule(Prefix, Suffix) { IsEnabled = IsActive },
            RenameRuleType.Replace => new ReplaceRule(SearchPattern, Replacement, MatchCase, UseRegex) { IsEnabled = IsActive },
            RenameRuleType.ChangeCase => new ChangeCaseRule(SelectedCaseIndex switch
            {
                1 => CaseMode.Uppercase,
                2 => CaseMode.TitleCase,
                _ => CaseMode.Lowercase
            })
            { IsEnabled = IsActive },
            RenameRuleType.Numbering => new NumberingRule(
                StartNumber,
                Step,
                PaddingFormat,
                SelectedPositionIndex == 1 ? NumberingPosition.Prefix : NumberingPosition.Suffix,
                Separator)
            { IsEnabled = IsActive },
            _ => throw new InvalidOperationException($"Unknown rule type: {RuleType}")
        };
    }
}
