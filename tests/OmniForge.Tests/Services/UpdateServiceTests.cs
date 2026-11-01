using System.Net;
using System.Security.Cryptography;
using System.Text;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Services;
using OmniForge.Services.Settings;
using OmniForge.Services.Updates;

namespace OmniForge.Tests.Services;

public sealed class UpdateServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ISettingsService _settingsService;
    private readonly IFileSystemService _fileSystemService;

    public UpdateServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OmniForgeUpdateTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        var localSettings = new LocalSettingsService();
        _settingsService = new SettingsService(localSettings);
        _fileSystemService = new FileSystemService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup failure
        }
    }

    private const string SampleGitHubReleasesJson = """
    [
      {
        "tag_name": "v0.3.0-preview.1",
        "name": "OmniForge 0.3.0 Preview 1",
        "draft": false,
        "prerelease": true,
        "published_at": "2026-11-01T10:00:00Z",
        "body": "Preview features: New hash engines and experimental UI.",
        "assets": [
          {
            "name": "OmniForge-v0.3.0-preview.1-x64.msix",
            "browser_download_url": "https://github.com/OmniForge/OmniForge/releases/download/v0.3.0-preview.1/OmniForge-v0.3.0-preview.1-x64.msix",
            "size": 45000000
          }
        ]
      },
      {
        "tag_name": "v0.2.0",
        "name": "OmniForge 0.2.0 Stable",
        "draft": false,
        "prerelease": false,
        "published_at": "2026-10-15T12:00:00Z",
        "body": "Major update: Text inspector, Lucky wheel, and Settings update.",
        "assets": [
          {
            "name": "OmniForge-v0.2.0-x64.msix",
            "browser_download_url": "https://github.com/OmniForge/OmniForge/releases/download/v0.2.0/OmniForge-v0.2.0-x64.msix",
            "size": 42000000
          }
        ]
      },
      {
        "tag_name": "v0.1.0",
        "name": "OmniForge 0.1.0 Initial",
        "draft": false,
        "prerelease": false,
        "published_at": "2026-09-01T08:00:00Z",
        "body": "Initial release",
        "assets": [
          {
            "name": "OmniForge-v0.1.0-x64.msix",
            "browser_download_url": "https://github.com/OmniForge/OmniForge/releases/download/v0.1.0/OmniForge-v0.1.0-x64.msix",
            "size": 40000000
          }
        ]
      }
    ]
    """;

    [Fact]
    public void ParseReleasesPayload_StableChannel_ShouldFilterOutPrereleases()
    {
        var result = UpdateService.ParseReleasesPayload(SampleGitHubReleasesJson, UpdateChannel.Stable);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("0.2.0", result.Value.Version);
        Assert.False(result.Value.IsPrerelease);
        Assert.Contains("v0.2.0", result.Value.DownloadUrl);
        Assert.Equal(42000000, result.Value.FileSizeBytes);
    }

    [Fact]
    public void ParseReleasesPayload_PreviewChannel_ShouldSelectNewerPreviewRelease()
    {
        var result = UpdateService.ParseReleasesPayload(SampleGitHubReleasesJson, UpdateChannel.Preview);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("0.3.0-preview.1", result.Value.Version);
        Assert.True(result.Value.IsPrerelease);
        Assert.Contains("v0.3.0-preview.1", result.Value.DownloadUrl);
    }

    [Fact]
    public async Task ChannelAndAutoCheckSettings_ShouldPersistAndRetrieveCorrectly()
    {
        var updateService = new UpdateService(_settingsService, _fileSystemService, updatesDirectory: _tempDirectory);

        var setChannelResult = await updateService.SetChannelAsync(UpdateChannel.Preview);
        Assert.True(setChannelResult.IsSuccess);

        var getChannelResult = await updateService.GetChannelAsync();
        Assert.True(getChannelResult.IsSuccess);
        Assert.Equal(UpdateChannel.Preview, getChannelResult.Value);

        var setAutoCheckResult = await updateService.SetAutoCheckOnStartupAsync(false);
        Assert.True(setAutoCheckResult.IsSuccess);

        var getAutoCheckResult = await updateService.GetAutoCheckOnStartupAsync();
        Assert.True(getAutoCheckResult.IsSuccess);
        Assert.False(getAutoCheckResult.Value);
    }

    [Fact]
    public async Task LaunchInstallerAsync_PathEscapingUpdatesDirectory_ShouldReturnSecurityFailure()
    {
        var updateService = new UpdateService(_settingsService, _fileSystemService, updatesDirectory: _tempDirectory);

        // Attempt path traversal outside _tempDirectory
        var evilPath = Path.Combine(_tempDirectory, "..", "System32", "evil.msix");

        var result = await updateService.LaunchInstallerAsync(evilPath);

        Assert.True(result.IsFailure);
        Assert.Contains("Security", result.Error.Code);
    }

    [Fact]
    public async Task LaunchInstallerAsync_DangerousExtension_ShouldReturnSecurityFailure()
    {
        var updateService = new UpdateService(_settingsService, _fileSystemService, updatesDirectory: _tempDirectory);

        var scriptFile = Path.Combine(_tempDirectory, "malicious.bat");
        await File.WriteAllTextAsync(scriptFile, "echo test");

        var result = await updateService.LaunchInstallerAsync(scriptFile);

        Assert.True(result.IsFailure);
        Assert.Equal("Security.InvalidInstallerExtension", result.Error.Code);
    }

    [Fact]
    public async Task DownloadUpdateAsync_InsecureHttpUrl_ShouldReturnSecurityFailure()
    {
        var updateService = new UpdateService(_settingsService, _fileSystemService, updatesDirectory: _tempDirectory);

        var insecureUpdate = new UpdateInfo(
            Version: "0.2.0",
            DisplayName: "Insecure Update",
            ReleaseNotes: "",
            DownloadUrl: "http://insecure.example.com/update.msix",
            FileSizeBytes: 1000);

        var result = await updateService.DownloadUpdateAsync(insecureUpdate);

        Assert.True(result.IsFailure);
        Assert.Equal("Security.InsecureDownloadUrl", result.Error.Code);
    }

    [Fact]
    public async Task DownloadUpdateAsync_ValidHttps_WithSha256Verification_ShouldVerifySuccessfully()
    {
        var payloadBytes = Encoding.UTF8.GetBytes("Mock MSIX Package Content for OmniForge Production");
        var expectedHash = Convert.ToHexString(SHA256.HashData(payloadBytes));

        var handler = new MockHttpMessageHandler(payloadBytes, HttpStatusCode.OK);
        using var client = new HttpClient(handler);

        var updateService = new UpdateService(
            _settingsService,
            _fileSystemService,
            httpClient: client,
            updatesDirectory: _tempDirectory);

        var updateInfo = new UpdateInfo(
            Version: "0.2.0",
            DisplayName: "Mock Valid Update",
            ReleaseNotes: "Verified release notes",
            DownloadUrl: "https://updates.omniforge.dev/download/OmniForge-0.2.0.msix",
            FileSizeBytes: payloadBytes.Length,
            Sha256Hash: expectedHash);

        var result = await updateService.DownloadUpdateAsync(updateInfo);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.Value));
        Assert.Equal(payloadBytes.Length, new FileInfo(result.Value).Length);
    }

    [Fact]
    public async Task DownloadUpdateAsync_CorruptedPayloadHashMismatch_ShouldRejectAndCleanUpTempFile()
    {
        var payloadBytes = Encoding.UTF8.GetBytes("Corrupted or tampered payload");
        var incorrectHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        var handler = new MockHttpMessageHandler(payloadBytes, HttpStatusCode.OK);
        using var client = new HttpClient(handler);

        var updateService = new UpdateService(
            _settingsService,
            _fileSystemService,
            httpClient: client,
            updatesDirectory: _tempDirectory);

        var updateInfo = new UpdateInfo(
            Version: "0.2.0",
            DisplayName: "Tampered Update",
            ReleaseNotes: "",
            DownloadUrl: "https://updates.omniforge.dev/download/OmniForge-0.2.0.msix",
            FileSizeBytes: payloadBytes.Length,
            Sha256Hash: incorrectHash);

        var result = await updateService.DownloadUpdateAsync(updateInfo);

        Assert.True(result.IsFailure);
        Assert.Equal("Security.HashMismatch", result.Error.Code);

        // Ensure temp file and corrupted file were wiped
        var filesInDir = Directory.GetFiles(_tempDirectory);
        Assert.Empty(filesInDir);
    }

    private sealed class MockHttpMessageHandler(byte[] content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content)
            };
            return Task.FromResult(response);
        }
    }
}

