using OmniForge.Core.Common;
using OmniForge.Core.Models;

namespace OmniForge.Core.Contracts;

/// <summary>
/// Contract for checking, downloading, verifying, and launching application updates.
/// Fully decoupled from UI and platform-specific window frameworks.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Gets the current installed application version string (e.g. "0.1.0-alpha").
    /// </summary>
    string CurrentAppVersion { get; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent successful or attempted update check.
    /// </summary>
    DateTimeOffset? LastCheckedTime { get; }

    /// <summary>
    /// Checks for available updates on the specified release channel.
    /// </summary>
    Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(
        UpdateChannel channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an update asset to a safe, isolated local location and verifies its cryptographic integrity.
    /// </summary>
    Task<Result<string>> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<UpdateProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes or hands off the downloaded verified installer to the operating system.
    /// </summary>
    Task<Result> LaunchInstallerAsync(
        string installerPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the configured update release channel.
    /// </summary>
    Task<Result<UpdateChannel>> GetChannelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates and persists the preferred update release channel.
    /// </summary>
    Task<Result> SetChannelAsync(UpdateChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether automatic update checks on startup are enabled.
    /// </summary>
    Task<Result<bool>> GetAutoCheckOnStartupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates whether automatic update checks on startup are enabled.
    /// </summary>
    Task<Result> SetAutoCheckOnStartupAsync(bool enabled, CancellationToken cancellationToken = default);
}

