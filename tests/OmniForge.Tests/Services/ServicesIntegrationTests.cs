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

