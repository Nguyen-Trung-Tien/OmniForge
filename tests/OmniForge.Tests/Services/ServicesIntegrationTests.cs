using OmniForge.Core.Contracts;
using OmniForge.Services;
using OmniForge.Services.Settings;

namespace OmniForge.Tests.Services;

public sealed class ServicesIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;

    public ServicesIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OmniForgeTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task FileSystemService_WriteAndRead_ShouldReturnSuccessResults()
    {
        var fileSystem = new FileSystemService();
        var filePath = Path.Combine(_tempDirectory, "subdir", "sample.txt");

        var writeResult = await fileSystem.WriteAllTextAsync(filePath, "Hello OmniForge Production");
        Assert.True(writeResult.IsSuccess);

        var existsResult = await fileSystem.FileExistsAsync(filePath);
        Assert.True(existsResult.IsSuccess);
        Assert.True(existsResult.Value);

        var readResult = await fileSystem.ReadAllTextAsync(filePath);
        Assert.True(readResult.IsSuccess);
        Assert.Equal("Hello OmniForge Production", readResult.Value);

        var sizeResult = await fileSystem.GetDirectorySizeAsync(_tempDirectory);
        Assert.True(sizeResult.IsSuccess);
        Assert.True(sizeResult.Value > 0);
    }

    [Fact]
    public async Task FileSystemService_ReadNonExistentFile_ShouldReturnFailureResultWithFileNotFoundCode()
    {
        var fileSystem = new FileSystemService();
        var missingPath = Path.Combine(_tempDirectory, "does_not_exist.txt");

        var result = await fileSystem.ReadAllTextAsync(missingPath);

        Assert.True(result.IsFailure);
        Assert.Equal("IO.FileNotFound", result.Error.Code);
    }

    [Fact]
    public async Task LocalSettingsService_SaveAndRead_ShouldPersistAcrossInstances()
    {
        var settingsFile = Path.Combine(_tempDirectory, "settings.json");
        var service1 = new LocalSettingsService(settingsFile);

        await service1.SaveSettingAsync("General.Theme", "Dark");
        await service1.SaveSettingAsync("General.MaxConcurrency", 8);

        var service2 = new LocalSettingsService(settingsFile);
        var theme = await service2.ReadSettingAsync("General.Theme", "Light");
        var maxConcurrency = await service2.ReadSettingAsync("General.MaxConcurrency", 1);
        var fallback = await service2.ReadSettingAsync("NonExistent.Key", 42);

        Assert.Equal("Dark", theme);
        Assert.Equal(8, maxConcurrency);
        Assert.Equal(42, fallback);
    }

    [Fact]
    public void ToolRegistryService_RegisterAndOrderModules_ShouldMaintainPriorityOrder()
    {
        var registry = new ToolRegistryService();
        var modB = new FakeModule("omniforge.b", "Beta", 20);
        var modA = new FakeModule("omniforge.a", "Alpha", 10);

        registry.RegisterModules([modB, modA]);

        Assert.Equal(2, registry.Modules.Count);
        Assert.Equal("omniforge.a", registry.Modules[0].Id);
        Assert.Equal("omniforge.b", registry.Modules[1].Id);
        Assert.NotNull(registry.GetModule("OMNIFORGE.A"));
        Assert.True(registry.UnregisterModule("omniforge.a"));
        Assert.Single(registry.Modules);
    }

    [Fact]
    public async Task SettingsService_ThemeWindowSizeAndTemplates_ShouldPersistAndNotify()
    {
        var settingsFile = Path.Combine(_tempDirectory, "settings_service.json");
        var localSettings = new LocalSettingsService(settingsFile);
        var settingsService = new SettingsService(localSettings);

        var changedKeys = new List<string>();
        settingsService.SettingChanged += (sender, key) => changedKeys.Add(key);

        // Theme test
        var initialTheme = await settingsService.GetThemeAsync();
        Assert.Equal("Default", initialTheme);

        await settingsService.SetThemeAsync("Dark");
        var updatedTheme = await settingsService.GetThemeAsync();
        Assert.Equal("Dark", updatedTheme);

        // Window size test
        var initialSize = await settingsService.GetWindowSizeAsync();
        Assert.Equal((1200, 800), initialSize);

        await settingsService.SetWindowSizeAsync(1440, 900);
        var updatedSize = await settingsService.GetWindowSizeAsync();
        Assert.Equal((1440, 900), updatedSize);

        // Last opened module test
        Assert.Null(await settingsService.GetLastOpenedModuleAsync());
        await settingsService.SetLastOpenedModuleAsync("batch-rename");
        Assert.Equal("batch-rename", await settingsService.GetLastOpenedModuleAsync());

        // Custom templates test
        var initialTemplates = await settingsService.GetUserTemplatesAsync("BatchRename");
        Assert.Empty(initialTemplates);

        await settingsService.SaveUserTemplateAsync("BatchRename", "Photos2026", "IMG_{date}_{index}");
        var templates = await settingsService.GetUserTemplatesAsync("BatchRename");
        Assert.Single(templates);
        Assert.Equal("IMG_{date}_{index}", templates["Photos2026"]);

        await settingsService.DeleteUserTemplateAsync("BatchRename", "Photos2026");
        var afterDelete = await settingsService.GetUserTemplatesAsync("BatchRename");
        Assert.Empty(afterDelete);

        Assert.Contains(SettingsService.ThemeSettingKey, changedKeys);
        Assert.Contains(SettingsService.LastOpenedModuleKey, changedKeys);
    }

    [Fact]
    public void NavigationService_ConfigureAndNavigate_Uninitialized_ShouldThrowOrReturnFalse()
    {
        var navService = new OmniForge.Services.Navigation.NavigationService();
        navService.Configure("Dashboard", typeof(object));

        // When unmapped key is provided, NavigateTo should return false
        var unmappedResult = navService.NavigateTo("NonExistentPageKey");
        Assert.False(unmappedResult);

        // When mapped key is provided but Frame is uninitialized, NavigateTo should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => navService.NavigateTo("Dashboard"));
    }

    [Fact]
    public void NavigationService_Configure_InvalidArgs_ShouldThrowArgumentException()
    {
        var navService = new OmniForge.Services.Navigation.NavigationService();

        Assert.Throws<ArgumentException>(() => navService.Configure(string.Empty, typeof(object)));
        Assert.Throws<ArgumentNullException>(() => navService.Configure("ValidKey", null!));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Ignore cleanup lock in test runner
            }
        }
    }

    private sealed record FakeModule(string Id, string DisplayName, int Priority) : IToolModule
    {
        public string Description => "Test module";
        public string IconGlyph => "\uE80F";
        public Type ViewType => typeof(object);
        public Type ViewModelType => typeof(object);
    }
}

