namespace OmniForge.Core.Models;

/// <summary>
/// Release stream channels for application updates.
/// </summary>
public enum UpdateChannel
{
    /// <summary>
    /// Officially validated stable production releases.
    /// </summary>
    Stable = 0,

    /// <summary>
    /// Preview and pre-release builds containing early features and experimental tools.
    /// </summary>
    Preview = 1
}

/// <summary>
/// Execution state of the application update life cycle.
/// </summary>
public enum UpdateState
{
    Idle = 0,
    Checking = 1,
    UpdateAvailable = 2,
    UpToDate = 3,
    Downloading = 4,
    ReadyToInstall = 5,
    Error = 6
}

/// <summary>
/// Represents metadata for an available remote application release.
/// </summary>
public sealed record UpdateInfo(
    string Version,
    string DisplayName,
    string ReleaseNotes,
    string DownloadUrl,
    long FileSizeBytes,
    string? Sha256Hash = null,
    DateTimeOffset? PublishedDate = null,
    bool IsPrerelease = false);

/// <summary>
/// Result of an update availability check.
/// </summary>
public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string CurrentVersion,
    UpdateInfo? LatestRelease,
    DateTimeOffset CheckedAtUtc);

/// <summary>
/// Progress information during update package download.
/// </summary>
public sealed record UpdateProgressInfo(
    long BytesReceived,
    long TotalBytesToReceive,
    double Percentage,
    double BytesPerSecond);

