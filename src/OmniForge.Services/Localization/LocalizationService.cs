using System.Globalization;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Services.Localization;

/// <summary>
/// Production localization service implementing <see cref="ILocalizationService"/> with
/// thread-safe in-memory bilingual catalogs, automatic OS culture detection, dynamic hot-reload,
/// and persistent user preferences via <see cref="ISettingsService"/>.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    public const string LanguageSettingKey = "General.Language";

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<string, string> _enStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _viStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncLock = new();

    public static readonly IReadOnlyList<LanguageItem> SupportedLanguagesList =
    [
        new(AppLanguage.English, "en-US", "English", "English (United States)"),
        new(AppLanguage.Vietnamese, "vi-VN", "Tiếng Việt", "Vietnamese (Vietnam)")
    ];

    public IReadOnlyList<LanguageItem> SupportedLanguages => SupportedLanguagesList;

    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

    public string CurrentCultureTag => CurrentLanguage == AppLanguage.Vietnamese ? "vi-VN" : "en-US";

    public event EventHandler<AppLanguage>? LanguageChanged;

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // 1. Initialize built-in catalogs
        PopulateEnglishCatalog();
        PopulateVietnameseCatalog();

        // 2. Default to OS culture before settings are loaded
        var isOsVietnamese = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("vi", StringComparison.OrdinalIgnoreCase);
        CurrentLanguage = isOsVietnamese ? AppLanguage.Vietnamese : AppLanguage.English;
        ApplyCulture(CurrentLanguage);

        // 3. Asynchronously load persisted setting
        _ = InitializeFromSettingsAsync();
    }

    private async Task InitializeFromSettingsAsync()
    {
        try
        {
            var saved = await _settingsService.GetSettingAsync<string?>(LanguageSettingKey, null).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse<AppLanguage>(saved, out var parsedLanguage))
            {
                if (parsedLanguage != CurrentLanguage)
                {
                    ApplyCulture(parsedLanguage);
                    LanguageChanged?.Invoke(this, parsedLanguage);
                }
            }
        }
        catch
        {
            // Defensive fault isolation: keep default culture if storage read fails
        }
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        lock (_syncLock)
        {
            var targetMap = CurrentLanguage == AppLanguage.Vietnamese ? _viStrings : _enStrings;

            // 1. Primary lookup
            if (targetMap.TryGetValue(key, out var localized) && !string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            // 2. Fallback to English if current language is not English
            if (CurrentLanguage != AppLanguage.English &&
                _enStrings.TryGetValue(key, out var enFallback) &&
                !string.IsNullOrWhiteSpace(enFallback))
            {
                return enFallback;
            }

            // 3. Fallback to key itself for developer visibility
            return $"[{key}]";
        }
    }

    public string GetFormattedString(string key, params object[] args)
    {
        var raw = GetString(key);
        if (args is null || args.Length == 0) return raw;

        try
        {
            var culture = CurrentLanguage == AppLanguage.Vietnamese
                ? CultureInfo.GetCultureInfo("vi-VN")
                : CultureInfo.GetCultureInfo("en-US");

            return string.Format(culture, raw, args);
        }
        catch (FormatException)
        {
            return raw;
        }
    }

    public async Task SetLanguageAsync(AppLanguage language, CancellationToken cancellationToken = default)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        ApplyCulture(language);
        await _settingsService.SetSettingAsync(LanguageSettingKey, language.ToString(), cancellationToken);
        LanguageChanged?.Invoke(this, language);
    }

    private void ApplyCulture(AppLanguage language)
    {
        CurrentLanguage = language;
        var tag = language == AppLanguage.Vietnamese ? "vi-VN" : "en-US";
        var culture = CultureInfo.GetCultureInfo(tag);

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    private void PopulateEnglishCatalog()
    {
        // Common
        _enStrings[LocalizationKeys.Common_Apply] = "Apply";
        _enStrings[LocalizationKeys.Common_Cancel] = "Cancel";
        _enStrings[LocalizationKeys.Common_Confirm] = "Confirm";
        _enStrings[LocalizationKeys.Common_Save] = "Save";
        _enStrings[LocalizationKeys.Common_Close] = "Close";
        _enStrings[LocalizationKeys.Common_Browse] = "Browse...";
        _enStrings[LocalizationKeys.Common_Copy] = "Copy";
        _enStrings[LocalizationKeys.Common_Delete] = "Delete";
        _enStrings[LocalizationKeys.Common_Refresh] = "Refresh";
        _enStrings[LocalizationKeys.Common_SearchPlaceholder] = "Search utilities...";
        _enStrings[LocalizationKeys.Common_Loading] = "Loading...";
        _enStrings[LocalizationKeys.Common_Processing] = "Processing {0} items...";
        _enStrings[LocalizationKeys.Common_Success] = "Operation completed successfully.";
        _enStrings[LocalizationKeys.Common_Failed] = "Operation failed.";
        _enStrings[LocalizationKeys.Common_Error] = "Error";
        _enStrings[LocalizationKeys.Common_Dismiss] = "Dismiss";
        _enStrings[LocalizationKeys.Common_Undo] = "Undo";
        _enStrings[LocalizationKeys.Common_Redo] = "Redo";
        _enStrings[LocalizationKeys.Common_Version] = "Version";

        // Module Titles and Descriptions (Both by Key and by Module ID)
        _enStrings[LocalizationKeys.Nav_AppTitle] = "OmniForge Utility Suite";
        _enStrings[LocalizationKeys.Nav_Dashboard] = "Dashboard";
        _enStrings[LocalizationKeys.Nav_Dashboard_Desc] = "System overview, active tools catalog, and quick launch shortcuts.";
        _enStrings["omniforge.dashboard.Title"] = "Dashboard";
        _enStrings["omniforge.dashboard.Desc"] = "System overview and utility quick-launch";

        _enStrings[LocalizationKeys.Nav_BatchRename] = "Batch Rename";
        _enStrings[LocalizationKeys.Nav_BatchRename_Desc] = "High-performance rule-based bulk file and directory renaming engine.";
        _enStrings["omniforge.batchrename.Title"] = "Batch Rename";
        _enStrings["omniforge.batchrename.Desc"] = "Chained transformation rules, live collision detection, and non-blocking batch renaming";

        _enStrings[LocalizationKeys.Nav_BatchFolder] = "Folder Creator";
        _enStrings[LocalizationKeys.Nav_BatchFolder_Desc] = "Scaffold complex folder hierarchies from templates or custom paths.";
        _enStrings["omniforge.batchfolder.Title"] = "Folder Creator";
        _enStrings["omniforge.batchfolder.Desc"] = "Scaffold complex folder structures from text paths, templates, or numbered series";

        _enStrings[LocalizationKeys.Nav_Checksum] = "Checksum & Hasher";
        _enStrings[LocalizationKeys.Nav_Checksum_Desc] = "Asynchronous cryptographic hashing (MD5, SHA-1, SHA-256, SHA-512).";
        _enStrings["Checksum.Title"] = "Checksum & Hasher";
        _enStrings["Checksum.Desc"] = "Asynchronous streaming file hash generation and checksum verification";
        _enStrings["omniforge.checksum.Title"] = "Checksum & Hasher";
        _enStrings["omniforge.checksum.Desc"] = "Asynchronous streaming file hash generation and checksum verification";

        _enStrings[LocalizationKeys.Nav_FileTools] = "File Utilities";
        _enStrings[LocalizationKeys.Nav_FileTools_Desc] = "Attribute inspection, timestamp updates, and file management.";
        _enStrings["omniforge.filetools.Title"] = "File Utilities";
        _enStrings["omniforge.filetools.Desc"] = "Attribute inspection, timestamp updates, and file management.";

        _enStrings[LocalizationKeys.Nav_TextInspector] = "Text Inspector";
        _enStrings[LocalizationKeys.Nav_TextInspector_Desc] = "JSON/XML formatting, validation, diff checking, and Base64 inspection.";
        _enStrings["TextInspector.Title"] = "Text Inspector";
        _enStrings["TextInspector.Desc"] = "Zero-allocation JSON/XML validation, minification, Base64/JWT decoding, and diffing";
        _enStrings["omniforge.textinspector.Title"] = "Text Inspector";
        _enStrings["omniforge.textinspector.Desc"] = "Zero-allocation JSON/XML validation, minification, Base64/JWT decoding, and diffing";

        _enStrings[LocalizationKeys.Nav_LuckyWheel] = "Lucky Wheel";
        _enStrings[LocalizationKeys.Nav_LuckyWheel_Desc] = "Win2D hardware-accelerated random selection decision wheel.";
        _enStrings["omniforge.luckywheel.Title"] = "Lucky Wheel";
        _enStrings["omniforge.luckywheel.Desc"] = "Hardware-accelerated random selection decision wheel with physics easing";

        _enStrings[LocalizationKeys.Nav_Settings] = "Settings";

        // Settings
        _enStrings[LocalizationKeys.Settings_Title] = "Settings";
        _enStrings[LocalizationKeys.Settings_Subtitle] = "Application preferences, language selection, and environment configurations.";
        _enStrings[LocalizationKeys.Settings_Appearance_Title] = "Appearance & System Backdrop";
        _enStrings[LocalizationKeys.Settings_Appearance_Subtitle] = "Customize Fluent MicaAlt window chrome and visual preferences.";
        _enStrings[LocalizationKeys.Settings_Theme] = "App Theme (Applied Live)";
        _enStrings[LocalizationKeys.Settings_Theme_Default] = "System Default";
        _enStrings[LocalizationKeys.Settings_Theme_Light] = "Light";
        _enStrings[LocalizationKeys.Settings_Theme_Dark] = "Dark";
        _enStrings[LocalizationKeys.Settings_MicaBackdrop] = "Enable MicaAlt System Backdrop";
        _enStrings[LocalizationKeys.Settings_Language_Title] = "Language & Regional Preferences";
        _enStrings[LocalizationKeys.Settings_Language_Subtitle] = "Choose the display language for the application user interface.";
        _enStrings[LocalizationKeys.Settings_Language_Label] = "Display Language";
        _enStrings[LocalizationKeys.Settings_Language_Note] = "Language changes take effect immediately without restarting the application.";
        _enStrings[LocalizationKeys.Settings_SavePreferences] = "Save Preferences";
        _enStrings[LocalizationKeys.Settings_SavedSuccess] = "Preferences saved successfully.";
        _enStrings[LocalizationKeys.Settings_ThemeUpdatedLive] = "Theme updated to '{0}' (applied live).";

        // Dashboard
        _enStrings[LocalizationKeys.Dashboard_Title] = "OmniForge Dashboard";
        _enStrings[LocalizationKeys.Dashboard_Subtitle] = "Centralized workspace & modular utility hub";
        _enStrings[LocalizationKeys.Dashboard_Status_Title] = "Modular System Status";
        _enStrings[LocalizationKeys.Dashboard_Status_Subtitle] = "Zero-touch auto-discovered IToolModule plugins";
        _enStrings[LocalizationKeys.Dashboard_Welcome] = "Welcome to OmniForge Utility Suite";
        _enStrings[LocalizationKeys.Dashboard_RegisteredTools] = "Registered Tools:";
        _enStrings[LocalizationKeys.Dashboard_Highlights_Title] = "Foundation Highlights";
        _enStrings[LocalizationKeys.Dashboard_Highlights_Subtitle] = "Clean Architecture & Fluent Design System";
        _enStrings[LocalizationKeys.Dashboard_Highlight1] = "• Windows App SDK & WinUI 3 with MicaAlt Backdrop";
        _enStrings[LocalizationKeys.Dashboard_Highlight2] = "• Shared Controls (ToolHeader, SectionCard, StatusFooter, EmptyState)";
        _enStrings[LocalizationKeys.Dashboard_Highlight3] = "• Decoupled DI services (FilePicker, Clipboard, FileSystem, Settings)";
        _enStrings[LocalizationKeys.Dashboard_Available_Title] = "Available Utilities";
        _enStrings[LocalizationKeys.Dashboard_Available_Subtitle] = "Select a tool module to launch directly";
        _enStrings[LocalizationKeys.Dashboard_Launch] = "Launch";

        // Shared Controls
        _enStrings[LocalizationKeys.Control_ToolHeader_ConfirmAndExecute] = "Confirm & Execute";
        _enStrings[LocalizationKeys.Control_ToolHeader_Cancel] = "Cancel";
        _enStrings[LocalizationKeys.Control_ToolHeader_ActionCompleted] = "Action Completed";
        _enStrings[LocalizationKeys.Control_ToolHeader_ActionConfirmation] = "Action Confirmation";
        _enStrings[LocalizationKeys.Control_StatusFooter_CancelTask] = "Cancel Task";
        _enStrings[LocalizationKeys.Control_StatusFooter_CancelTaskToolTip] = "Cancel the currently running task (Esc)";
        _enStrings[LocalizationKeys.Control_StatusFooter_Ready] = "Ready.";
        _enStrings[LocalizationKeys.Control_StatusFooter_Processing] = "Processing...";
        _enStrings[LocalizationKeys.Control_EmptyState_NoData] = "No data available";

        // Lucky Wheel Module
        _enStrings[LocalizationKeys.LuckyWheel_Title] = "Lucky Wheel";
        _enStrings[LocalizationKeys.LuckyWheel_Subtitle] = "Hardware-accelerated random selection decision wheel with physics easing";
        _enStrings[LocalizationKeys.LuckyWheel_ItemsListTitle] = "Items List";
        _enStrings[LocalizationKeys.LuckyWheel_ItemsListSubtitle] = "Manage wheel slices, adjust odds, or import from files";
        _enStrings[LocalizationKeys.LuckyWheel_ImportFileBtn] = "Import File...";
        _enStrings[LocalizationKeys.LuckyWheel_ImportFileToolTip] = "Import items from Excel (.xlsx), CSV, TXT";
        _enStrings[LocalizationKeys.LuckyWheel_Presets] = "Presets";
        _enStrings[LocalizationKeys.LuckyWheel_Actions] = "Actions";

        _enStrings[LocalizationKeys.LuckyWheel_Preset_Decision] = "Decision Maker (Yes/No/Maybe)";
        _enStrings[LocalizationKeys.LuckyWheel_Preset_Lunch] = "Lunch Chooser (Pizza, Pho, Sushi...)";
        _enStrings[LocalizationKeys.LuckyWheel_Preset_Raffle] = "Prize Raffle (Prize 1, 2, 3...)";
        _enStrings[LocalizationKeys.LuckyWheel_Preset_Standup] = "Team Standup";
        _enStrings[LocalizationKeys.LuckyWheel_Preset_Dice] = "Lucky Dice (1-6)";

        _enStrings[LocalizationKeys.LuckyWheel_Action_SortName] = "Sort by Name (A-Z)";
        _enStrings[LocalizationKeys.LuckyWheel_Action_SortWeight] = "Sort by Weight Descending";
        _enStrings[LocalizationKeys.LuckyWheel_Action_Shuffle] = "Shuffle Order";
        _enStrings[LocalizationKeys.LuckyWheel_Action_Equalize] = "Equalize Weights (1.0)";
        _enStrings[LocalizationKeys.LuckyWheel_Action_EnableAll] = "Enable All Items";
        _enStrings[LocalizationKeys.LuckyWheel_Action_DisableAll] = "Disable All Items";
        _enStrings[LocalizationKeys.LuckyWheel_Action_CopyCsv] = "Copy CSV List";
        _enStrings[LocalizationKeys.LuckyWheel_Action_ClearAll] = "Clear All Items";

        _enStrings[LocalizationKeys.LuckyWheel_QuickAdd_Placeholder] = "Enter item label (or multiple items separated by comma)...";
        _enStrings[LocalizationKeys.LuckyWheel_QuickAdd_AddBtn] = "Add";
        _enStrings[LocalizationKeys.LuckyWheel_QuickAdd_AddToolTip] = "Add item (Enter)";
        _enStrings[LocalizationKeys.LuckyWheel_Filter_Placeholder] = "Quick search items in list...";
        _enStrings[LocalizationKeys.LuckyWheel_Filter_Header] = "Filter list";
        _enStrings[LocalizationKeys.LuckyWheel_Badge_Active] = "Active: {0}";
        _enStrings[LocalizationKeys.LuckyWheel_Badge_Weight] = "Weight: {0}";
        _enStrings[LocalizationKeys.LuckyWheel_Badge_Count] = "{0} / {1} items";

        _enStrings[LocalizationKeys.LuckyWheel_Col_Color] = "Color";
        _enStrings[LocalizationKeys.LuckyWheel_Col_Name] = "Item Label";
        _enStrings[LocalizationKeys.LuckyWheel_Col_Weight] = "Weight";
        _enStrings[LocalizationKeys.LuckyWheel_Col_Odds] = "Odds";
        _enStrings[LocalizationKeys.LuckyWheel_Col_Toggle] = "Enabled";
        _enStrings[LocalizationKeys.LuckyWheel_Col_Delete] = "Delete";
        _enStrings[LocalizationKeys.LuckyWheel_Stepper_Decrease] = "Decrease weight (-0.5)";
        _enStrings[LocalizationKeys.LuckyWheel_Stepper_Increase] = "Increase weight (+0.5)";
        _enStrings[LocalizationKeys.LuckyWheel_Stepper_Tip] = "Enter numeric weight";

        _enStrings[LocalizationKeys.LuckyWheel_Empty_Title] = "No items yet";
        _enStrings[LocalizationKeys.LuckyWheel_Empty_Subtitle] = "Enter item names above and press Enter, or click 'Import File...' to load from Excel/TXT.";

        _enStrings[LocalizationKeys.LuckyWheel_SpinBtn] = "SPIN";
        _enStrings[LocalizationKeys.LuckyWheel_StopBtn] = "STOP";
        _enStrings[LocalizationKeys.LuckyWheel_Duration] = "Spin Duration: {0:F1}s";
        _enStrings[LocalizationKeys.LuckyWheel_Physics] = "Physics Profile";
        _enStrings[LocalizationKeys.LuckyWheel_Sound] = "Sound Effects";
        _enStrings[LocalizationKeys.LuckyWheel_Confetti] = "Celebration Confetti";
        _enStrings[LocalizationKeys.LuckyWheel_HistoryBtn] = "History ({0})";

        _enStrings[LocalizationKeys.LuckyWheel_History_Title] = "Winner History";
        _enStrings[LocalizationKeys.LuckyWheel_History_Clear] = "Clear all";
        _enStrings[LocalizationKeys.LuckyWheel_History_CopyAll] = "Copy full history";
        _enStrings[LocalizationKeys.LuckyWheel_History_Empty] = "No winning history yet.";

        _enStrings[LocalizationKeys.LuckyWheel_Winner_Title] = "WHEEL RESULT";
        _enStrings[LocalizationKeys.LuckyWheel_Winner_SpinAgain] = "Spin Again";
        _enStrings[LocalizationKeys.LuckyWheel_Winner_RemoveAndSpin] = "Remove & Spin";
        _enStrings[LocalizationKeys.LuckyWheel_Winner_RemoveTip] = "Remove this winner from wheel and spin again for next round";
        _enStrings[LocalizationKeys.LuckyWheel_Winner_CopyName] = "Copy Name";
        _enStrings[LocalizationKeys.LuckyWheel_Winner_Close] = "Close";

        _enStrings[LocalizationKeys.LuckyWheel_Import_Title] = "Import Items List";
        _enStrings[LocalizationKeys.LuckyWheel_Import_Subtitle] = "Supports Excel (.xlsx), CSV, Text (.txt) or batch raw text paste.";
        _enStrings[LocalizationKeys.LuckyWheel_Import_TabFile] = "From File (Excel / CSV / TXT)";
        _enStrings[LocalizationKeys.LuckyWheel_Import_NoFile] = "No file selected. Supports .xlsx, .csv, .tsv, .txt";
        _enStrings[LocalizationKeys.LuckyWheel_Import_ChooseFile] = "Choose file from computer...";
        _enStrings[LocalizationKeys.LuckyWheel_Import_TabPaste] = "Paste Text Directly";
        _enStrings[LocalizationKeys.LuckyWheel_Import_PasteTip] = "One item per line. Optional weight: 'Item: 2' or 'Item (3)' or 'Item, 1.5'.";
        _enStrings[LocalizationKeys.LuckyWheel_Import_Mode] = "Import mode:";
        _enStrings[LocalizationKeys.LuckyWheel_Import_Replace] = "Replace existing list (Replace)";
        _enStrings[LocalizationKeys.LuckyWheel_Import_Append] = "Append to current list (Append)";
        _enStrings[LocalizationKeys.LuckyWheel_Import_Cancel] = "Cancel";
        _enStrings[LocalizationKeys.LuckyWheel_Import_Confirm] = "Confirm Import";

        // Other Feature Modules
        _enStrings[LocalizationKeys.BatchRename_Title] = "Batch File & Path Renamer";
        _enStrings[LocalizationKeys.BatchRename_Subtitle] = "Chained transformation rules, live collision detection, and non-blocking batch renaming";
        _enStrings[LocalizationKeys.BatchFolder_Title] = "Batch Folder Creator";
        _enStrings[LocalizationKeys.BatchFolder_Subtitle] = "Scaffold complex folder structures from text paths, templates, or numbered series";
        _enStrings[LocalizationKeys.Checksum_Title] = "Checksum & Hash Generator";
        _enStrings[LocalizationKeys.Checksum_Subtitle] = "Asynchronous streaming file hash generation and checksum verification";
        _enStrings[LocalizationKeys.FileTools_Title] = "File Utilities & Inspector";
        _enStrings[LocalizationKeys.FileTools_Subtitle] = "Inspect metadata, update timestamps, and manage bulk file attributes";
        _enStrings[LocalizationKeys.TextInspector_Title] = "Text & Payload Inspector";
        _enStrings[LocalizationKeys.TextInspector_Subtitle] = "Zero-allocation JSON/XML validation, minification, Base64/JWT decoding, and diffing";
    }

    private void PopulateVietnameseCatalog()
    {
        // Common
        _viStrings[LocalizationKeys.Common_Apply] = "Áp dụng";
        _viStrings[LocalizationKeys.Common_Cancel] = "Hủy";
        _viStrings[LocalizationKeys.Common_Confirm] = "Xác nhận";
        _viStrings[LocalizationKeys.Common_Save] = "Lưu thay đổi";
        _viStrings[LocalizationKeys.Common_Close] = "Đóng";
        _viStrings[LocalizationKeys.Common_Browse] = "Duyệt thư mục...";
        _viStrings[LocalizationKeys.Common_Copy] = "Sao chép";
        _viStrings[LocalizationKeys.Common_Delete] = "Xóa";
        _viStrings[LocalizationKeys.Common_Refresh] = "Làm mới";
        _viStrings[LocalizationKeys.Common_SearchPlaceholder] = "Tìm kiếm tiện ích...";
        _viStrings[LocalizationKeys.Common_Loading] = "Đang tải...";
        _viStrings[LocalizationKeys.Common_Processing] = "Đang xử lý {0} mục...";
        _viStrings[LocalizationKeys.Common_Success] = "Thao tác hoàn thành thành công.";
        _viStrings[LocalizationKeys.Common_Failed] = "Thao tác thất bại.";
        _viStrings[LocalizationKeys.Common_Error] = "Lỗi";
        _viStrings[LocalizationKeys.Common_Dismiss] = "Bỏ qua";
        _viStrings[LocalizationKeys.Common_Undo] = "Hoàn tác";
        _viStrings[LocalizationKeys.Common_Redo] = "Làm lại";
        _viStrings[LocalizationKeys.Common_Version] = "Phiên bản";

        // Module Titles and Descriptions (Both by Key and by Module ID)
        _viStrings[LocalizationKeys.Nav_AppTitle] = "Bộ Tiện Ích OmniForge";
        _viStrings[LocalizationKeys.Nav_Dashboard] = "Bảng điều khiển";
        _viStrings[LocalizationKeys.Nav_Dashboard_Desc] = "Tổng quan hệ thống, danh mục công cụ đang hoạt động và lối tắt khởi chạy nhanh.";
        _viStrings["omniforge.dashboard.Title"] = "Bảng điều khiển";
        _viStrings["omniforge.dashboard.Desc"] = "Tổng quan hệ thống và lối tắt khởi chạy công cụ nhanh chóng";

        _viStrings[LocalizationKeys.Nav_BatchRename] = "Đổi tên hàng loạt";
        _viStrings[LocalizationKeys.Nav_BatchRename_Desc] = "Công cụ đổi tên tệp và thư mục hàng loạt theo quy tắc hiệu năng cao.";
        _viStrings["omniforge.batchrename.Title"] = "Đổi tên hàng loạt";
        _viStrings["omniforge.batchrename.Desc"] = "Quy tắc chuyển đổi linh hoạt, phát hiện xung đột trực tiếp và đổi tên hàng loạt không khóa giao diện";

        _viStrings[LocalizationKeys.Nav_BatchFolder] = "Tạo thư mục hàng loạt";
        _viStrings[LocalizationKeys.Nav_BatchFolder_Desc] = "Tạo cây thư mục phức tạp theo mẫu định sẵn hoặc đường dẫn tùy chỉnh.";
        _viStrings["omniforge.batchfolder.Title"] = "Tạo thư mục hàng loạt";
        _viStrings["omniforge.batchfolder.Desc"] = "Tạo cấu trúc thư mục từ danh sách văn bản, mẫu cấu hình hoặc chuỗi số tăng dần";

        _viStrings[LocalizationKeys.Nav_Checksum] = "Mã băm & Toàn vẹn";
        _viStrings[LocalizationKeys.Nav_Checksum_Desc] = "Tạo mã băm bảo mật bất đồng bộ (MD5, SHA-1, SHA-256, SHA-512).";
        _viStrings["Checksum.Title"] = "Mã băm & Toàn vẹn";
        _viStrings["Checksum.Desc"] = "Tính toán mã băm luồng bất đồng bộ và kiểm tra tính toàn vẹn dữ liệu";
        _viStrings["omniforge.checksum.Title"] = "Mã băm & Toàn vẹn";
        _viStrings["omniforge.checksum.Desc"] = "Tính toán mã băm luồng bất đồng bộ và kiểm tra tính toàn vẹn dữ liệu";

        _viStrings[LocalizationKeys.Nav_FileTools] = "Công cụ tệp tin";
        _viStrings[LocalizationKeys.Nav_FileTools_Desc] = "Kiểm tra thuộc tính tệp, cập nhật mốc thời gian và quản lý tệp tin.";
        _viStrings["omniforge.filetools.Title"] = "Công cụ tệp tin";
        _viStrings["omniforge.filetools.Desc"] = "Kiểm tra thuộc tính tệp, cập nhật mốc thời gian và quản lý tệp tin.";

        _viStrings[LocalizationKeys.Nav_TextInspector] = "Kiểm tra văn bản";
        _viStrings[LocalizationKeys.Nav_TextInspector_Desc] = "Định dạng JSON/XML, kiểm tra hợp lệ, so sánh văn bản và giải mã Base64.";
        _viStrings["TextInspector.Title"] = "Kiểm tra văn bản";
        _viStrings["TextInspector.Desc"] = "Định dạng JSON/XML chuẩn xác, nén gọn, giải mã Base64/JWT và so sánh khác biệt văn bản";
        _viStrings["omniforge.textinspector.Title"] = "Kiểm tra văn bản";
        _viStrings["omniforge.textinspector.Desc"] = "Định dạng JSON/XML chuẩn xác, nén gọn, giải mã Base64/JWT và so sánh khác biệt văn bản";

        _viStrings[LocalizationKeys.Nav_LuckyWheel] = "Vòng quay may mắn";
        _viStrings[LocalizationKeys.Nav_LuckyWheel_Desc] = "Vòng quay quyết định ngẫu nhiên tăng tốc phần cứng bằng Win2D.";
        _viStrings["omniforge.luckywheel.Title"] = "Vòng quay may mắn";
        _viStrings["omniforge.luckywheel.Desc"] = "Vòng quay quyết định ngẫu nhiên hỗ trợ gia tốc phần cứng Win2D và vật lý mượt mà";

        _viStrings[LocalizationKeys.Nav_Settings] = "Cài đặt";

        // Settings
        _viStrings[LocalizationKeys.Settings_Title] = "Cài đặt";
        _viStrings[LocalizationKeys.Settings_Subtitle] = "Tùy chọn ứng dụng, lựa chọn ngôn ngữ và cấu hình môi trường.";
        _viStrings[LocalizationKeys.Settings_Appearance_Title] = "Giao diện & Hiệu ứng nền";
        _viStrings[LocalizationKeys.Settings_Appearance_Subtitle] = "Tùy chỉnh hiệu ứng MicaAlt Fluent và phong cách hiển thị.";
        _viStrings[LocalizationKeys.Settings_Theme] = "Chủ đề giao diện (Áp dụng ngay)";
        _viStrings[LocalizationKeys.Settings_Theme_Default] = "Theo hệ thống";
        _viStrings[LocalizationKeys.Settings_Theme_Light] = "Sáng";
        _viStrings[LocalizationKeys.Settings_Theme_Dark] = "Tối";
        _viStrings[LocalizationKeys.Settings_MicaBackdrop] = "Bật hiệu ứng nền MicaAlt";
        _viStrings[LocalizationKeys.Settings_Language_Title] = "Ngôn ngữ & Vùng";
        _viStrings[LocalizationKeys.Settings_Language_Subtitle] = "Chọn ngôn ngữ hiển thị cho giao diện người dùng của ứng dụng.";
        _viStrings[LocalizationKeys.Settings_Language_Label] = "Ngôn ngữ hiển thị";
        _viStrings[LocalizationKeys.Settings_Language_Note] = "Thay đổi ngôn ngữ có hiệu lực ngay lập tức mà không cần khởi động lại ứng dụng.";
        _viStrings[LocalizationKeys.Settings_SavePreferences] = "Lưu thiết lập";
        _viStrings[LocalizationKeys.Settings_SavedSuccess] = "Thiết lập đã được lưu thành công.";
        _viStrings[LocalizationKeys.Settings_ThemeUpdatedLive] = "Chủ đề đã được cập nhật thành '{0}' (áp dụng ngay).";

        // Dashboard
        _viStrings[LocalizationKeys.Dashboard_Title] = "Bảng điều khiển OmniForge";
        _viStrings[LocalizationKeys.Dashboard_Subtitle] = "Không gian làm việc tập trung & trung tâm tiện ích mô-đun";
        _viStrings[LocalizationKeys.Dashboard_Status_Title] = "Trạng thái hệ thống";
        _viStrings[LocalizationKeys.Dashboard_Status_Subtitle] = "Tự động phát hiện và kích hoạt mô-đun công cụ";
        _viStrings[LocalizationKeys.Dashboard_Welcome] = "Chào mừng bạn đến với Bộ tiện ích OmniForge";
        _viStrings[LocalizationKeys.Dashboard_RegisteredTools] = "Công cụ đã đăng ký:";
        _viStrings[LocalizationKeys.Dashboard_Highlights_Title] = "Điểm nổi bật kiến trúc";
        _viStrings[LocalizationKeys.Dashboard_Highlights_Subtitle] = "Kiến trúc sạch & Hệ thống Fluent Design";
        _viStrings[LocalizationKeys.Dashboard_Highlight1] = "• Windows App SDK & WinUI 3 với hiệu ứng nền MicaAlt";
        _viStrings[LocalizationKeys.Dashboard_Highlight2] = "• Thành phần dùng chung (ToolHeader, SectionCard, StatusFooter, EmptyState)";
        _viStrings[LocalizationKeys.Dashboard_Highlight3] = "• Dịch vụ DI phân tách (FilePicker, Clipboard, FileSystem, Settings)";
        _viStrings[LocalizationKeys.Dashboard_Available_Title] = "Công cụ khả dụng";
        _viStrings[LocalizationKeys.Dashboard_Available_Subtitle] = "Chọn một công cụ để khởi chạy trực tiếp";
        _viStrings[LocalizationKeys.Dashboard_Launch] = "Khởi chạy";

        // Shared Controls
        _viStrings[LocalizationKeys.Control_ToolHeader_ConfirmAndExecute] = "Xác nhận & Thực thi";
        _viStrings[LocalizationKeys.Control_ToolHeader_Cancel] = "Hủy";
        _viStrings[LocalizationKeys.Control_ToolHeader_ActionCompleted] = "Hoàn thành tác vụ";
        _viStrings[LocalizationKeys.Control_ToolHeader_ActionConfirmation] = "Xác nhận tác vụ";
        _viStrings[LocalizationKeys.Control_StatusFooter_CancelTask] = "Hủy tác vụ";
        _viStrings[LocalizationKeys.Control_StatusFooter_CancelTaskToolTip] = "Hủy bỏ tác vụ đang chạy (Phím Esc)";
        _viStrings[LocalizationKeys.Control_StatusFooter_Ready] = "Sẵn sàng.";
        _viStrings[LocalizationKeys.Control_StatusFooter_Processing] = "Đang xử lý...";
        _viStrings[LocalizationKeys.Control_EmptyState_NoData] = "Chưa có dữ liệu";

        // Lucky Wheel Module
        _viStrings[LocalizationKeys.LuckyWheel_Title] = "Vòng quay may mắn";
        _viStrings[LocalizationKeys.LuckyWheel_Subtitle] = "Vòng quay quyết định ngẫu nhiên hỗ trợ gia tốc phần cứng Win2D và vật lý mượt mà";
        _viStrings[LocalizationKeys.LuckyWheel_ItemsListTitle] = "Danh Sách Đối Tượng";
        _viStrings[LocalizationKeys.LuckyWheel_ItemsListSubtitle] = "Quản lý các ô trong vòng quay, chỉnh tỉ lệ hoặc nhập từ file";
        _viStrings[LocalizationKeys.LuckyWheel_ImportFileBtn] = "Nhập File...";
        _viStrings[LocalizationKeys.LuckyWheel_ImportFileToolTip] = "Nhập danh sách từ Excel (.xlsx), CSV, TXT";
        _viStrings[LocalizationKeys.LuckyWheel_Presets] = "Mẫu sẵn";
        _viStrings[LocalizationKeys.LuckyWheel_Actions] = "Thao tác";

        _viStrings[LocalizationKeys.LuckyWheel_Preset_Decision] = "Vòng quay Quyết định (Yes/No/Maybe)";
        _viStrings[LocalizationKeys.LuckyWheel_Preset_Lunch] = "Chọn món ăn trưa (Pizza, Phở, Sushi...)";
        _viStrings[LocalizationKeys.LuckyWheel_Preset_Raffle] = "Bốc thăm trúng thưởng (Giải 1, 2, 3...)";
        _viStrings[LocalizationKeys.LuckyWheel_Preset_Standup] = "Thành viên nhóm (Team Standup)";
        _viStrings[LocalizationKeys.LuckyWheel_Preset_Dice] = "Xúc xắc may mắn (1-6)";

        _viStrings[LocalizationKeys.LuckyWheel_Action_SortName] = "Sắp xếp theo tên (A-Z)";
        _viStrings[LocalizationKeys.LuckyWheel_Action_SortWeight] = "Sắp xếp theo trọng số giảm dần";
        _viStrings[LocalizationKeys.LuckyWheel_Action_Shuffle] = "Trộn ngẫu nhiên thứ tự";
        _viStrings[LocalizationKeys.LuckyWheel_Action_Equalize] = "Đặt lại trọng số đều nhau (1.0)";
        _viStrings[LocalizationKeys.LuckyWheel_Action_EnableAll] = "Bật tất cả đối tượng";
        _viStrings[LocalizationKeys.LuckyWheel_Action_DisableAll] = "Tắt tất cả đối tượng";
        _viStrings[LocalizationKeys.LuckyWheel_Action_CopyCsv] = "Sao chép danh sách CSV";
        _viStrings[LocalizationKeys.LuckyWheel_Action_ClearAll] = "Xóa toàn bộ danh sách";

        _viStrings[LocalizationKeys.LuckyWheel_QuickAdd_Placeholder] = "Nhập tên đối tượng (hoặc nhiều mục cách nhau bởi dấu phẩy)...";
        _viStrings[LocalizationKeys.LuckyWheel_QuickAdd_AddBtn] = "Thêm";
        _viStrings[LocalizationKeys.LuckyWheel_QuickAdd_AddToolTip] = "Thêm đối tượng (Enter)";
        _viStrings[LocalizationKeys.LuckyWheel_Filter_Placeholder] = "Tìm kiếm nhanh trong danh sách...";
        _viStrings[LocalizationKeys.LuckyWheel_Filter_Header] = "Lọc danh sách";
        _viStrings[LocalizationKeys.LuckyWheel_Badge_Active] = "Đang bật: {0}";
        _viStrings[LocalizationKeys.LuckyWheel_Badge_Weight] = "Trọng số: {0}";
        _viStrings[LocalizationKeys.LuckyWheel_Badge_Count] = "{0} / {1} mục";

        _viStrings[LocalizationKeys.LuckyWheel_Col_Color] = "Màu sắc";
        _viStrings[LocalizationKeys.LuckyWheel_Col_Name] = "Tên đối tượng";
        _viStrings[LocalizationKeys.LuckyWheel_Col_Weight] = "Trọng số";
        _viStrings[LocalizationKeys.LuckyWheel_Col_Odds] = "Tỉ lệ";
        _viStrings[LocalizationKeys.LuckyWheel_Col_Toggle] = "Bật / Tắt";
        _viStrings[LocalizationKeys.LuckyWheel_Col_Delete] = "Xóa";
        _viStrings[LocalizationKeys.LuckyWheel_Stepper_Decrease] = "Giảm trọng số (-0.5)";
        _viStrings[LocalizationKeys.LuckyWheel_Stepper_Increase] = "Tăng trọng số (+0.5)";
        _viStrings[LocalizationKeys.LuckyWheel_Stepper_Tip] = "Nhập trọng số trực tiếp";

        _viStrings[LocalizationKeys.LuckyWheel_Empty_Title] = "Chưa có đối tượng nào";
        _viStrings[LocalizationKeys.LuckyWheel_Empty_Subtitle] = "Nhập tên đối tượng ở trên và nhấn Enter, hoặc bấm 'Nhập File...' để tải danh sách từ Excel/TXT.";

        _viStrings[LocalizationKeys.LuckyWheel_SpinBtn] = "QUAY";
        _viStrings[LocalizationKeys.LuckyWheel_StopBtn] = "DỪNG";
        _viStrings[LocalizationKeys.LuckyWheel_Duration] = "Thời gian quay: {0:F1}s";
        _viStrings[LocalizationKeys.LuckyWheel_Physics] = "Hiệu ứng vật lý";
        _viStrings[LocalizationKeys.LuckyWheel_Sound] = "Hiệu ứng âm thanh";
        _viStrings[LocalizationKeys.LuckyWheel_Confetti] = "Pháo hoa chúc mừng";
        _viStrings[LocalizationKeys.LuckyWheel_HistoryBtn] = "Lịch sử ({0})";

        _viStrings[LocalizationKeys.LuckyWheel_History_Title] = "Lịch Sử Trúng Thưởng";
        _viStrings[LocalizationKeys.LuckyWheel_History_Clear] = "Xóa hết";
        _viStrings[LocalizationKeys.LuckyWheel_History_CopyAll] = "Sao chép toàn bộ lịch sử";
        _viStrings[LocalizationKeys.LuckyWheel_History_Empty] = "Chưa có lượt quay trúng nào.";

        _viStrings[LocalizationKeys.LuckyWheel_Winner_Title] = "KẾT QUẢ VÒNG QUAY";
        _viStrings[LocalizationKeys.LuckyWheel_Winner_SpinAgain] = "Quay tiếp";
        _viStrings[LocalizationKeys.LuckyWheel_Winner_RemoveAndSpin] = "Loại bỏ & Quay";
        _viStrings[LocalizationKeys.LuckyWheel_Winner_RemoveTip] = "Loại bỏ người trúng này khỏi vòng quay và quay tiếp cho lượt sau";
        _viStrings[LocalizationKeys.LuckyWheel_Winner_CopyName] = "Sao chép tên";
        _viStrings[LocalizationKeys.LuckyWheel_Winner_Close] = "Đóng";

        _viStrings[LocalizationKeys.LuckyWheel_Import_Title] = "Nhập Danh Sách Đối Tượng";
        _viStrings[LocalizationKeys.LuckyWheel_Import_Subtitle] = "Hỗ trợ file Excel (.xlsx), CSV, Text (.txt) hoặc dán văn bản hàng loạt.";
        _viStrings[LocalizationKeys.LuckyWheel_Import_TabFile] = "Từ File (Excel / CSV / TXT)";
        _viStrings[LocalizationKeys.LuckyWheel_Import_NoFile] = "Chưa chọn file nào. Hỗ trợ .xlsx, .csv, .tsv, .txt";
        _viStrings[LocalizationKeys.LuckyWheel_Import_ChooseFile] = "Chọn file từ máy tính...";
        _viStrings[LocalizationKeys.LuckyWheel_Import_TabPaste] = "Dán văn bản trực tiếp";
        _viStrings[LocalizationKeys.LuckyWheel_Import_PasteTip] = "Mỗi dòng là một đối tượng. Có thể kèm trọng số: 'Tên mục: 2' hoặc 'Tên (3)' hoặc 'Tên, 1.5'.";
        _viStrings[LocalizationKeys.LuckyWheel_Import_Mode] = "Chế độ nhập:";
        _viStrings[LocalizationKeys.LuckyWheel_Import_Replace] = "Thay thế danh sách cũ (Replace)";
        _viStrings[LocalizationKeys.LuckyWheel_Import_Append] = "Nối tiếp vào danh sách hiện tại (Append)";
        _viStrings[LocalizationKeys.LuckyWheel_Import_Cancel] = "Hủy";
        _viStrings[LocalizationKeys.LuckyWheel_Import_Confirm] = "Xác nhận nhập";

        // Other Feature Modules
        _viStrings[LocalizationKeys.BatchRename_Title] = "Đổi tên tệp & thư mục hàng loạt";
        _viStrings[LocalizationKeys.BatchRename_Subtitle] = "Quy tắc chuyển đổi linh hoạt, phát hiện xung đột trực tiếp và đổi tên hàng loạt không khóa giao diện";
        _viStrings[LocalizationKeys.BatchFolder_Title] = "Tạo thư mục hàng loạt";
        _viStrings[LocalizationKeys.BatchFolder_Subtitle] = "Tạo cấu trúc thư mục từ danh sách văn bản, mẫu cấu hình hoặc chuỗi số tăng dần";
        _viStrings[LocalizationKeys.Checksum_Title] = "Mã băm & Toàn vẹn dữ liệu";
        _viStrings[LocalizationKeys.Checksum_Subtitle] = "Tính toán mã băm luồng bất đồng bộ và kiểm tra tính toàn vẹn dữ liệu";
        _viStrings[LocalizationKeys.FileTools_Title] = "Công cụ & Thuộc tính tệp tin";
        _viStrings[LocalizationKeys.FileTools_Subtitle] = "Kiểm tra thuộc tính tệp, cập nhật mốc thời gian và quản lý tệp tin";
        _viStrings[LocalizationKeys.TextInspector_Title] = "Kiểm tra & Định dạng văn bản";
        _viStrings[LocalizationKeys.TextInspector_Subtitle] = "Định dạng JSON/XML chuẩn xác, nén gọn, giải mã Base64/JWT và so sánh khác biệt văn bản";
    }
}
