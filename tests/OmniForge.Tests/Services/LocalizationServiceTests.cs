using OmniForge.Core.Common;
using OmniForge.Core.Models;
using OmniForge.Services;
using OmniForge.Services.Localization;
using OmniForge.Services.Settings;

namespace OmniForge.Tests.Services;

public sealed class LocalizationServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;

    public LocalizationServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OmniForgeLocTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "settings.json");
    }

    [Fact]
    public void SupportedLanguages_ShouldContainEnglishAndVietnamese()
    {
        var localSettings = new LocalSettingsService(_settingsFilePath);
        var settingsService = new SettingsService(localSettings);
        var locService = new LocalizationService(settingsService);

        Assert.NotNull(locService.SupportedLanguages);
        Assert.Equal(2, locService.SupportedLanguages.Count);

        var en = locService.SupportedLanguages.FirstOrDefault(l => l.Language == AppLanguage.English);
        var vi = locService.SupportedLanguages.FirstOrDefault(l => l.Language == AppLanguage.Vietnamese);

        Assert.NotNull(en);
        Assert.Equal("en-US", en.Bcp47Tag);
        Assert.Equal("English", en.NativeName);

        Assert.NotNull(vi);
        Assert.Equal("vi-VN", vi.Bcp47Tag);
        Assert.Equal("Tiếng Việt", vi.NativeName);
    }

    [Fact]
    public async Task GetString_WhenEnglish_ShouldReturnEnglishStrings()
    {
        var localSettings = new LocalSettingsService(_settingsFilePath);
        var settingsService = new SettingsService(localSettings);
        var locService = new LocalizationService(settingsService);

        await locService.SetLanguageAsync(AppLanguage.English);

        Assert.Equal("Save", locService.GetString(LocalizationKeys.Common_Save));
        Assert.Equal("Cancel", locService.GetString(LocalizationKeys.Common_Cancel));
        Assert.Equal("Settings", locService.GetString(LocalizationKeys.Settings_Title));
        Assert.Equal("Batch Rename", locService.GetString(LocalizationKeys.Nav_BatchRename));
    }

    [Fact]
    public async Task GetString_WhenVietnamese_ShouldReturnProfessionalVietnameseStrings()
    {
        var localSettings = new LocalSettingsService(_settingsFilePath);
        var settingsService = new SettingsService(localSettings);
        var locService = new LocalizationService(settingsService);

        await locService.SetLanguageAsync(AppLanguage.Vietnamese);

        Assert.Equal("Lưu thay đổi", locService.GetString(LocalizationKeys.Common_Save));
        Assert.Equal("Hủy", locService.GetString(LocalizationKeys.Common_Cancel));
        Assert.Equal("Cài đặt", locService.GetString(LocalizationKeys.Settings_Title));
        Assert.Equal("Đổi tên hàng loạt", locService.GetString(LocalizationKeys.Nav_BatchRename));
        Assert.Equal("Vòng quay may mắn", locService.GetString(LocalizationKeys.Nav_LuckyWheel));
    }

    [Fact]
    public async Task SetLanguageAsync_ShouldTriggerLanguageChangedEventAndPersist()
    {
        var localSettings = new LocalSettingsService(_settingsFilePath);
        var settingsService = new SettingsService(localSettings);
        var locService = new LocalizationService(settingsService);

        AppLanguage? eventReceivedLanguage = null;
        locService.LanguageChanged += (_, lang) => eventReceivedLanguage = lang;

        await locService.SetLanguageAsync(AppLanguage.Vietnamese);

        Assert.Equal(AppLanguage.Vietnamese, locService.CurrentLanguage);
        Assert.Equal("vi-VN", locService.CurrentCultureTag);
        Assert.Equal(AppLanguage.Vietnamese, eventReceivedLanguage);

        // Verify persistence in settings storage
        var persistedCode = await settingsService.GetSettingAsync<string>(LocalizationService.LanguageSettingKey, string.Empty);
        Assert.Equal(AppLanguage.Vietnamese.ToString(), persistedCode);
    }

    [Fact]
    public async Task GetFormattedString_ShouldInterpolateArgumentsCorrectly()
    {
        var localSettings = new LocalSettingsService(_settingsFilePath);
        var settingsService = new SettingsService(localSettings);
        var locService = new LocalizationService(settingsService);

        await locService.SetLanguageAsync(AppLanguage.English);
        var formattedEn = locService.GetFormattedString(LocalizationKeys.Common_Processing, 42);
        Assert.Equal("Processing 42 items...", formattedEn);

        await locService.SetLanguageAsync(AppLanguage.Vietnamese);
        var formattedVi = locService.GetFormattedString(LocalizationKeys.Common_Processing, 42);
        Assert.Equal("Đang xử lý 42 mục...", formattedVi);
    }

    [Fact]
    public async Task GetString_WhenKeyUnknown_ShouldReturnKeyInBrackets()
    {
        var localSettings = new LocalSettingsService(_settingsFilePath);
        var settingsService = new SettingsService(localSettings);
        var locService = new LocalizationService(settingsService);

        await locService.SetLanguageAsync(AppLanguage.Vietnamese);
        var result = locService.GetString("NonExistent_Key_XYZ");

        Assert.Equal("[NonExistent_Key_XYZ]", result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore test cleanup exceptions
        }
    }
}

