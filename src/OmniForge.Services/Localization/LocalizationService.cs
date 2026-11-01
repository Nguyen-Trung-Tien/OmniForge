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
        await _settingsService.SetSettingAsync(LanguageSettingKey, language.ToString(), cancellationToken).ConfigureAwait(false);
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
    }
}
