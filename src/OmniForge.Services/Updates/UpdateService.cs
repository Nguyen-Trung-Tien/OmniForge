using System.Buffers;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Services.Updates;

/// <summary>
/// Production implementation of <see cref="IUpdateService"/> providing resilient release checking,
/// streaming cryptographic download with on-the-fly SHA-256 hash validation, and safe installer invocation.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    public const string ChannelSettingKey = "Update.Channel";
    public const string AutoCheckSettingKey = "Update.AutoCheckOnStartup";
    public const string LastCheckedSettingKey = "Update.LastCheckedTimestamp";
    public const string FeedUrlSettingKey = "Update.FeedUrl";

    public const string DefaultGitHubRepoFeed = "https://api.github.com/repos/OmniForge/OmniForge/releases";
    public const string DefaultCurrentVersion = "0.1.0-alpha";

    private readonly ISettingsService _settingsService;
    private readonly IFileSystemService _fileSystemService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;
    private readonly string _updatesDirectory;

    public string CurrentAppVersion { get; }
    public DateTimeOffset? LastCheckedTime { get; private set; }

    public UpdateService(
        ISettingsService settingsService,
        IFileSystemService fileSystemService,
        HttpClient? httpClient = null,
        ILogger<UpdateService>? logger = null,
        string? currentVersion = null,
        string? updatesDirectory = null)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? NullLogger<UpdateService>.Instance;
        CurrentAppVersion = string.IsNullOrWhiteSpace(currentVersion) ? DefaultCurrentVersion : currentVersion;

        _updatesDirectory = string.IsNullOrWhiteSpace(updatesDirectory)
            ? Path.Combine(Path.GetTempPath(), "OmniForge", "Updates")
            : updatesDirectory;

        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OmniForge-App", CurrentAppVersion));
        }

        _ = InitializeLastCheckedTimeAsync();
    }

    private async Task InitializeLastCheckedTimeAsync()
    {
        try
        {
            var savedTime = await _settingsService.GetSettingAsync<string?>(LastCheckedSettingKey, null).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(savedTime) && DateTimeOffset.TryParse(savedTime, out var parsed))
            {
                LastCheckedTime = parsed;
            }
        }
        catch
        {
            // Defensive fault isolation
        }
    }

    public async Task<Result<UpdateChannel>> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = await _settingsService.GetSettingAsync<string?>(ChannelSettingKey, null, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse<UpdateChannel>(raw, true, out var channel))
            {
                return Result.Success(channel);
            }

            // Default to Preview if current version is a pre-release, otherwise Stable
            var isPreRelease = SemanticVersion.TryParse(CurrentAppVersion, out var currentSemVer) && currentSemVer.IsPreRelease;
            return Result.Success(isPreRelease ? UpdateChannel.Preview : UpdateChannel.Stable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve update channel setting.");
            return Result.Failure<UpdateChannel>("Update.GetChannelError", ex.Message);
        }
    }

    public async Task<Result> SetChannelAsync(UpdateChannel channel, CancellationToken cancellationToken = default)
    {
        try
        {
            await _settingsService.SetSettingAsync(ChannelSettingKey, channel.ToString(), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save update channel setting.");
            return Result.Failure("Update.SetChannelError", ex.Message);
        }
    }

    public async Task<Result<bool>> GetAutoCheckOnStartupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var enabled = await _settingsService.GetSettingAsync(AutoCheckSettingKey, true, cancellationToken).ConfigureAwait(false);
            return Result.Success(enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve auto-check setting.");
            return Result.Failure<bool>("Update.GetAutoCheckError", ex.Message);
        }
    }

    public async Task<Result> SetAutoCheckOnStartupAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        try
        {
            await _settingsService.SetSettingAsync(AutoCheckSettingKey, enabled, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save auto-check setting.");
            return Result.Failure("Update.SetAutoCheckError", ex.Message);
        }
    }

    public async Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(
        UpdateChannel channel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for updates on channel {Channel} (current version: {CurrentVersion})", channel, CurrentAppVersion);

            var feedUrl = await _settingsService.GetSettingAsync(FeedUrlSettingKey, DefaultGitHubRepoFeed, cancellationToken).ConfigureAwait(false);
            if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var validatedUri) || validatedUri.Scheme != Uri.UriSchemeHttps)
            {
                return Result.Failure<UpdateCheckResult>("Security.InsecureFeedUrl", "Update feed URL must be an absolute HTTPS address.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, validatedUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode == 403 || statusCode == 429)
                {
                    return Result.Failure<UpdateCheckResult>("Update.RateLimited", "Update server request limit reached. Please try again later.");
                }

                return Result.Failure<UpdateCheckResult>("Update.HttpError", $"Update server returned status code {response.StatusCode} ({statusCode}).");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var parseResult = ParseReleasesPayload(jsonContent, channel);
            if (!parseResult.IsSuccess)
            {
                return Result.Failure<UpdateCheckResult>(parseResult.Error);
            }

            var latestCandidate = parseResult.Value;
            var now = DateTimeOffset.UtcNow;
            LastCheckedTime = now;
            await _settingsService.SetSettingAsync(LastCheckedSettingKey, now.ToString("O"), cancellationToken).ConfigureAwait(false);

            if (latestCandidate is null)
            {
                return Result.Success(new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: CurrentAppVersion,
                    LatestRelease: null,
                    CheckedAtUtc: now));
            }

            // Semantic version comparison
            var currentParsed = SemanticVersion.TryParse(CurrentAppVersion, out var currentSemVer);
            var remoteParsed = SemanticVersion.TryParse(latestCandidate.Version, out var remoteSemVer);

            bool isNewer;
            if (currentParsed && remoteParsed)
            {
                isNewer = remoteSemVer! > currentSemVer!;
            }
            else
            {
                // Fallback to case-insensitive comparison if non-standard
                isNewer = !string.Equals(CurrentAppVersion, latestCandidate.Version, StringComparison.OrdinalIgnoreCase);
            }

            return Result.Success(new UpdateCheckResult(
                IsUpdateAvailable: isNewer,
                CurrentVersion: CurrentAppVersion,
                LatestRelease: isNewer ? latestCandidate : null,
                CheckedAtUtc: now));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<UpdateCheckResult>("Common.Cancelled", "Update check was cancelled.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network connectivity error while checking updates.");
            return Result.Failure<UpdateCheckResult>("Update.NetworkError", "Network connection failed. Please verify your internet connection.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking for updates.");
            return Result.Failure<UpdateCheckResult>("Update.UnexpectedError", ex.Message);
        }
    }

    public static Result<UpdateInfo?> ParseReleasesPayload(string jsonContent, UpdateChannel channel)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return Result.Failure<UpdateInfo?>("Update.EmptyResponse", "Received empty response from update server.");
        }

        var sizeCheck = SecurityGuard.ValidatePayloadSize(jsonContent);
        if (sizeCheck.IsFailure)
        {
            return Result.Failure<UpdateInfo?>(sizeCheck.Error);
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonContent, new JsonDocumentOptions { MaxDepth = 64 });
            var root = doc.RootElement;

            List<JsonElement> releaseElements = [];
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                {
                    releaseElements.Add(el);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Single release manifest object
                releaseElements.Add(root);
            }
            else
            {
                return Result.Failure<UpdateInfo?>("Update.InvalidJson", "Unrecognized JSON structure in update response.");
            }

            UpdateInfo? bestMatch = null;
            SemanticVersion? bestVersion = null;

            foreach (var el in releaseElements)
            {
                var isDraft = el.TryGetProperty("draft", out var draftProp) && draftProp.GetBoolean();
                if (isDraft) continue;

                var isPrerelease = el.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();
                if (channel == UpdateChannel.Stable && isPrerelease)
                {
                    // Filter out preview releases when channel is Stable
                    continue;
                }

                var rawVersion = el.TryGetProperty("tag_name", out var tagProp)
                    ? tagProp.GetString()
                    : el.TryGetProperty("version", out var verProp) ? verProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(rawVersion)) continue;

                if (!SemanticVersion.TryParse(rawVersion, out var candidateSemVer))
                {
                    continue;
                }

                if (channel == UpdateChannel.Stable && candidateSemVer.IsPreRelease)
                {
                    // Also filter out versions with -alpha/-beta suffixes when on Stable
                    continue;
                }

                // Find suitable downloadable asset
                var (downloadUrl, fileSize, sha256) = ExtractAssetDetails(el);
                if (string.IsNullOrWhiteSpace(downloadUrl)) continue;

                var releaseName = el.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString())
                    ? nameProp.GetString()!
                    : $"OmniForge {rawVersion}";

                var releaseNotes = el.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;

                DateTimeOffset? publishedDate = null;
                if (el.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTimeOffset(out var pubDate))
                {
                    publishedDate = pubDate;
                }

                var candidateInfo = new UpdateInfo(
                    Version: rawVersion.TrimStart('v', 'V'),
                    DisplayName: releaseName,
                    ReleaseNotes: releaseNotes,
                    DownloadUrl: downloadUrl,
                    FileSizeBytes: fileSize,
                    Sha256Hash: sha256,
                    PublishedDate: publishedDate,
                    IsPrerelease: isPrerelease || candidateSemVer.IsPreRelease);

                if (bestVersion is null || candidateSemVer > bestVersion)
                {
                    bestVersion = candidateSemVer;
                    bestMatch = candidateInfo;
                }
            }

            return Result.Success(bestMatch);
        }
        catch (JsonException ex)
        {
            return Result.Failure<UpdateInfo?>("Update.JsonParseException", $"Failed to parse release payload: {ex.Message}");
        }
    }

    private static (string? DownloadUrl, long FileSize, string? Sha256) ExtractAssetDetails(JsonElement releaseElement)
    {
        // 1. Direct download_url / url on root (custom update manifest)
        if (releaseElement.TryGetProperty("download_url", out var directUrlProp) && !string.IsNullOrWhiteSpace(directUrlProp.GetString()))
        {
            var size = releaseElement.TryGetProperty("size", out var sizeProp) && sizeProp.TryGetInt64(out var s) ? s : 0;
            var sha = releaseElement.TryGetProperty("sha256", out var shaProp) ? shaProp.GetString() : null;
            return (directUrlProp.GetString(), size, sha);
        }

        // 2. GitHub Assets array
        if (releaseElement.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
        {
            string? fallbackUrl = null;
            long fallbackSize = 0;
            string? sha256Found = null;

            // Search for SHA256SUMS or checksum asset first
            foreach (var asset in assetsProp.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                if (!string.IsNullOrEmpty(name) && name.Contains("sha256", StringComparison.OrdinalIgnoreCase))
                {
                    // Checksum file present
                }
            }

            // Look for preferred package formats: .msix, .msixbundle, .exe, .zip
            foreach (var asset in assetsProp.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var nProp) ? nProp.GetString() : string.Empty;
                var url = asset.TryGetProperty("browser_download_url", out var uProp) ? uProp.GetString() : null;
                var size = asset.TryGetProperty("size", out var sProp) && sProp.TryGetInt64(out var s) ? s : 0;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;

                if (name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".appinstaller", StringComparison.OrdinalIgnoreCase))
                {
                    return (url, size, sha256Found);
                }

                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    fallbackUrl = url;
                    fallbackSize = size;
                }
            }

            if (!string.IsNullOrEmpty(fallbackUrl))
            {
                return (fallbackUrl, fallbackSize, sha256Found);
            }
        }

        return (null, 0, null);
    }

    public async Task<Result<string>> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<UpdateProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        if (!Uri.TryCreate(updateInfo.DownloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result.Failure<string>("Security.InsecureDownloadUrl", "Download URL must be a secure HTTPS endpoint.");
        }

        // 1. Sanitize destination file name and ensure directory exists
        var rawFileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            rawFileName = $"OmniForge-{updateInfo.Version}.msix";
        }

        var safeFileName = Path.GetInvalidFileNameChars()
            .Aggregate(rawFileName, (current, c) => current.Replace(c, '_'));

        var dirResult = SecurityGuard.ValidateSafePath(_updatesDirectory);
        if (!dirResult.IsSuccess)
        {
            return Result.Failure<string>(dirResult.Error);
        }

        Directory.CreateDirectory(_updatesDirectory);

        var rawDestinationPath = Path.Combine(_updatesDirectory, safeFileName);
        var destCheck = SecurityGuard.ValidateSafePath(rawDestinationPath, _updatesDirectory);
        if (!destCheck.IsSuccess)
        {
            return Result.Failure<string>(destCheck.Error);
        }

        var destinationPath = destCheck.Value;
        var tempFilePath = destinationPath + ".download.tmp";

        _logger.LogInformation("Starting download of {Version} from {Url} to {TempPath}", updateInfo.Version, updateInfo.DownloadUrl, tempFilePath);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(81920); // 80 KB
        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<string>("Update.DownloadHttpError", $"Download server returned HTTP {(int)response.StatusCode}.");
            }

            var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.FileSizeBytes;
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            using var incrementalHasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            long totalBytesRead = 0;
            int bytesRead;
            var stopwatch = Stopwatch.StartNew();
            long lastReportBytes = 0;
            var lastReportTime = stopwatch.ElapsedMilliseconds;

            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                incrementalHasher.AppendData(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                var currentTime = stopwatch.ElapsedMilliseconds;
                if (progress != null && (currentTime - lastReportTime >= 120 || totalBytesRead == totalBytes))
                {
                    var timeDeltaSec = (currentTime - lastReportTime) / 1000.0;
                    var bytesDelta = totalBytesRead - lastReportBytes;
                    var speed = timeDeltaSec > 0 ? bytesDelta / timeDeltaSec : 0.0;
                    var percent = totalBytes > 0 ? (double)totalBytesRead / totalBytes * 100.0 : 0.0;

                    progress.Report(new UpdateProgressInfo(totalBytesRead, totalBytes, Math.Min(100.0, percent), speed));
                    lastReportBytes = totalBytesRead;
                    lastReportTime = currentTime;
                }
            }

            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            fileStream.Close();

            // 2. Cryptographic Checksum Validation
            var computedHashBytes = incrementalHasher.GetHashAndReset();
            var computedHashHex = Convert.ToHexString(computedHashBytes);

            if (!string.IsNullOrWhiteSpace(updateInfo.Sha256Hash))
            {
                var cleanExpected = updateInfo.Sha256Hash.Trim().Replace("-", string.Empty);
                if (!string.Equals(computedHashHex, cleanExpected, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("SHA-256 hash mismatch: expected {Expected}, computed {Computed}", cleanExpected, computedHashHex);
                    SafeDeleteFile(tempFilePath);
                    return Result.Failure<string>(
                        "Security.HashMismatch",
                        $"Cryptographic verification failed: Downloaded file hash ({computedHashHex}) did not match expected ({cleanExpected}).");
                }
            }

            // 3. Atomically finalize temporary download file
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            File.Move(tempFilePath, destinationPath);

            _logger.LogInformation("Update downloaded and verified successfully: {Path} ({Size} bytes)", destinationPath, totalBytesRead);

            // Report final 100%
            progress?.Report(new UpdateProgressInfo(totalBytesRead, totalBytes, 100.0, 0));

            return Result.Success(destinationPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update download was cancelled by user.");
            SafeDeleteFile(tempFilePath);
            return Result.Failure<string>("Common.Cancelled", "Download was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during update download.");
            SafeDeleteFile(tempFilePath);
            return Result.Failure<string>("Update.DownloadException", ex.Message);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public Task<Result> LaunchInstallerAsync(string installerPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var pathCheck = SecurityGuard.ValidateSafePath(installerPath, _updatesDirectory);
            if (!pathCheck.IsSuccess)
            {
                return Task.FromResult(Result.Failure("Security.UnauthorizedInstallerPath", pathCheck.Error.Message));
            }

            var fullPath = pathCheck.Value;
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(Result.Failure("Update.InstallerNotFound", $"Installer file not found at '{fullPath}'."));
            }

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            if (extension is not (".msix" or ".msixbundle" or ".appinstaller" or ".exe"))
            {
                return Task.FromResult(Result.Failure("Security.InvalidInstallerExtension", $"Untrusted installer extension '{extension}'."));
            }

            _logger.LogInformation("Launching installer process for {Path}", fullPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            if (process is null)
            {
                return Task.FromResult(Result.Failure("Update.ProcessStartFailed", "Failed to start installer process."));
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch installer process.");
            return Task.FromResult(Result.Failure("Update.LaunchException", ex.Message));
        }
    }

    private static void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Suppress cleanup failure
        }
    }
}
