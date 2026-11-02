using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.App.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private readonly IDispatcherService _dispatcherService;
    private readonly IUpdateService _updateService;

    private bool _isInitialLoading = true;
    private bool _isUpdatingSelection;
    private bool _disposed;
    private CancellationTokenSource? _downloadCts;
    private CancellationTokenSource? _statusNoticeCts;
    private CancellationTokenSource? _upToDateCts;
    private UpdateInfo? _latestUpdateInfo;

    // --- Appearance & System Preferences ---

    [ObservableProperty]
    public partial string AppVersion { get; set; } = "0.1.0-alpha";

    [ObservableProperty]
    public partial bool EnableMicaAlt { get; set; } = true;

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = "Default";

    public IReadOnlyList<string> ThemeOptions { get; } = ["Default", "Light", "Dark"];

    public IReadOnlyList<LanguageItem> SupportedLanguages => _localizationService.SupportedLanguages;

    [ObservableProperty]
    public partial LanguageItem? SelectedLanguage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusNotice))]
    public partial string StatusNotice { get; set; } = string.Empty;

    public bool HasStatusNotice
    {
        get => !string.IsNullOrEmpty(StatusNotice);
        set
        {
            if (!value)
            {
                StatusNotice = string.Empty;
            }
        }
    }

    // --- Application Updates Observables ---

    [ObservableProperty]
    public partial UpdateState CurrentUpdateState { get; set; } = UpdateState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckUpdates))]
    public partial bool IsCheckingUpdates { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckUpdates))]
    public partial bool IsDownloadingUpdate { get; set; }

    public bool CanCheckUpdates => !IsCheckingUpdates && !IsDownloadingUpdate;

    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsUpToDate { get; set; }

    [ObservableProperty]
    public partial bool IsReadyToInstall { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateError))]
    public partial string UpdateErrorMessage { get; set; } = string.Empty;

    public bool HasUpdateError => !string.IsNullOrWhiteSpace(UpdateErrorMessage);

    [ObservableProperty]
    public partial string LatestVersionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LatestReleaseTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReleaseNotes))]
    public partial string ReleaseNotesText { get; set; } = string.Empty;

    public bool HasReleaseNotes => !string.IsNullOrWhiteSpace(ReleaseNotesText);

    [ObservableProperty]
    public partial string DownloadSizeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double DownloadProgressPercentage { get; set; }

    [ObservableProperty]
    public partial string DownloadProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DownloadSpeedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastCheckedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UpdateChannel SelectedChannel { get; set; } = UpdateChannel.Stable;

    public IReadOnlyList<UpdateChannel> ChannelOptions { get; } = [UpdateChannel.Stable, UpdateChannel.Preview];

    [ObservableProperty]
    public partial bool AutoCheckOnStartup { get; set; } = true;

    [ObservableProperty]
    public partial string DownloadedInstallerPath { get; set; } = string.Empty;

    // --- Localized Labels ---

    [ObservableProperty] public partial string TitleText { get; set; } = string.Empty;
    [ObservableProperty] public partial string SubtitleText { get; set; } = string.Empty;
    [ObservableProperty] public partial string LanguageTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string LanguageSubtitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string LanguageLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string LanguageNote { get; set; } = string.Empty;
    [ObservableProperty] public partial string AppearanceTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string AppearanceSubtitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string ThemeLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string BackdropLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string VersionLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string SavePreferencesLabel { get; set; } = string.Empty;

    // Localized Update Section Labels
    [ObservableProperty] public partial string UpdatesTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesSubtitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesCurrentVersionLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesCheckNowButtonText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesCheckingText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesUpToDateText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesAvailableText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesAvailableDesc { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesChannelLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesAutoCheckLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesAutoCheckDesc { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesDownloadButtonText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesCancelDownloadButtonText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesReadyToInstallText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesReadyToInstallDesc { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesInstallButtonText { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesReleaseNotesLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesRetryButtonText { get; set; } = string.Empty;

    public SettingsViewModel(
        ISettingsService settingsService,
        ILocalizationService localizationService,
        IDispatcherService dispatcherService,
        IUpdateService updateService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        AppVersion = _updateService.CurrentAppVersion;
        _localizationService.LanguageChanged += OnLanguageChanged;
        UpdateLocalizedStrings();

        _ = LoadSettingsAsync(CancellationToken.None);
    }

    private void UpdateLocalizedStrings()
    {
        TitleText = _localizationService.GetString(LocalizationKeys.Settings_Title);
        SubtitleText = _localizationService.GetString(LocalizationKeys.Settings_Subtitle);
        LanguageTitle = _localizationService.GetString(LocalizationKeys.Settings_Language_Title);
        LanguageSubtitle = _localizationService.GetString(LocalizationKeys.Settings_Language_Subtitle);
        LanguageLabel = _localizationService.GetString(LocalizationKeys.Settings_Language_Label);
        LanguageNote = _localizationService.GetString(LocalizationKeys.Settings_Language_Note);
        AppearanceTitle = _localizationService.GetString(LocalizationKeys.Settings_Appearance_Title);
        AppearanceSubtitle = _localizationService.GetString(LocalizationKeys.Settings_Appearance_Subtitle);
        ThemeLabel = _localizationService.GetString(LocalizationKeys.Settings_Theme);
        BackdropLabel = _localizationService.GetString(LocalizationKeys.Settings_MicaBackdrop);
        VersionLabel = _localizationService.GetString(LocalizationKeys.Common_Version);
        SavePreferencesLabel = _localizationService.GetString(LocalizationKeys.Settings_SavePreferences);

        // Update Section Strings
        UpdatesTitle = _localizationService.GetString(LocalizationKeys.Settings_Updates_Title);
        UpdatesSubtitle = _localizationService.GetString(LocalizationKeys.Settings_Updates_Subtitle);
        UpdatesCurrentVersionLabel = _localizationService.GetString(LocalizationKeys.Settings_Updates_CurrentVersion);
        UpdatesCheckNowButtonText = _localizationService.GetString(LocalizationKeys.Settings_Updates_CheckNow);
        UpdatesCheckingText = _localizationService.GetString(LocalizationKeys.Settings_Updates_Checking);
        UpdatesUpToDateText = _localizationService.GetString(LocalizationKeys.Settings_Updates_UpToDate);
        UpdatesAvailableDesc = _localizationService.GetString(LocalizationKeys.Settings_Updates_AvailableDesc);
        UpdatesChannelLabel = _localizationService.GetString(LocalizationKeys.Settings_Updates_Channel);
        UpdatesAutoCheckLabel = _localizationService.GetString(LocalizationKeys.Settings_Updates_AutoCheck);
        UpdatesAutoCheckDesc = _localizationService.GetString(LocalizationKeys.Settings_Updates_AutoCheckDesc);
        UpdatesDownloadButtonText = _localizationService.GetString(LocalizationKeys.Settings_Updates_Download);
        UpdatesCancelDownloadButtonText = _localizationService.GetString(LocalizationKeys.Settings_Updates_CancelDownload);
        UpdatesReadyToInstallText = _localizationService.GetString(LocalizationKeys.Settings_Updates_ReadyToInstall);
        UpdatesReadyToInstallDesc = _localizationService.GetString(LocalizationKeys.Settings_Updates_ReadyToInstallDesc);
        UpdatesInstallButtonText = _localizationService.GetString(LocalizationKeys.Settings_Updates_InstallAndRestart);
        UpdatesReleaseNotesLabel = _localizationService.GetString(LocalizationKeys.Settings_Updates_ReleaseNotes);
        UpdatesRetryButtonText = _localizationService.GetString(LocalizationKeys.Settings_Updates_Retry);

        if (!string.IsNullOrEmpty(LatestVersionText))
        {
            UpdatesAvailableText = _localizationService.GetFormattedString(LocalizationKeys.Settings_Updates_Available, LatestVersionText);
        }

        UpdateLastCheckedDisplay();
    }

    private void UpdateLastCheckedDisplay()
    {
        if (_updateService.LastCheckedTime is { } time)
        {
            var localTime = time.ToLocalTime();
            LastCheckedText = _localizationService.GetFormattedString(
                LocalizationKeys.Settings_Updates_LastChecked,
                localTime.ToString("g", CultureInfo.CurrentCulture));
        }
        else
        {
            LastCheckedText = _localizationService.GetString(LocalizationKeys.Settings_Updates_LastCheckedNever);
        }
    }

    private void SetStatusNotice(string message)
    {
        _statusNoticeCts?.Cancel();
        _statusNoticeCts?.Dispose();
        _statusNoticeCts = new CancellationTokenSource();
        var token = _statusNoticeCts.Token;

        StatusNotice = message;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000, token);
                if (!token.IsCancellationRequested)
                {
                    _dispatcherService.TryEnqueue(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            StatusNotice = string.Empty;
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (!_isInitialLoading && !string.IsNullOrWhiteSpace(value))
        {
            _ = RunSettingUpdateSafeAsync(async () =>
            {
                await _settingsService.SetThemeAsync(value);
                SetStatusNotice(_localizationService.GetFormattedString(LocalizationKeys.Settings_ThemeUpdatedLive, value));
            });
        }
    }

    partial void OnEnableMicaAltChanged(bool value)
    {
        if (!_isInitialLoading)
        {
            _ = RunSettingUpdateSafeAsync(() => _settingsService.SetSettingAsync("General.EnableMicaAlt", value));
        }
    }

    partial void OnSelectedLanguageChanged(LanguageItem? value)
    {
        if (!_isInitialLoading && !_isUpdatingSelection && value is not null && value.Language != _localizationService.CurrentLanguage)
        {
            _ = RunSettingUpdateSafeAsync(async () =>
            {
                await _localizationService.SetLanguageAsync(value.Language);
                SetStatusNotice(_localizationService.GetString(LocalizationKeys.Settings_SavedSuccess));
            });
        }
    }

    partial void OnSelectedChannelChanged(UpdateChannel value)
    {
        if (!_isInitialLoading)
        {
            _ = RunSettingUpdateSafeAsync(() => _updateService.SetChannelAsync(value));
        }
    }

    partial void OnAutoCheckOnStartupChanged(bool value)
    {
        if (!_isInitialLoading)
        {
            _ = RunSettingUpdateSafeAsync(() => _updateService.SetAutoCheckOnStartupAsync(value));
        }
    }

    private async Task RunSettingUpdateSafeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusNotice = ex.Message;
        }
    }

    private void OnLanguageChanged(object? sender, AppLanguage newLanguage)
    {
        _dispatcherService.TryEnqueue(() =>
        {
            _isUpdatingSelection = true;
            try
            {
                SelectedLanguage = _localizationService.SupportedLanguages.FirstOrDefault(l => l.Language == newLanguage);
                UpdateLocalizedStrings();
                OnPropertyChanged(nameof(SupportedLanguages));
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        });
    }

    [RelayCommand]
    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _isInitialLoading = true;
            EnableMicaAlt = await _settingsService.GetSettingAsync("General.EnableMicaAlt", true, cancellationToken);
            SelectedTheme = await _settingsService.GetThemeAsync(cancellationToken);
            SelectedLanguage = _localizationService.SupportedLanguages.FirstOrDefault(l => l.Language == _localizationService.CurrentLanguage);

            var channelResult = await _updateService.GetChannelAsync(cancellationToken);
            SelectedChannel = channelResult.IsSuccess ? channelResult.Value : UpdateChannel.Stable;

            var autoCheckResult = await _updateService.GetAutoCheckOnStartupAsync(cancellationToken);
            AutoCheckOnStartup = !autoCheckResult.IsSuccess || autoCheckResult.Value;

            UpdateLastCheckedDisplay();
        }
        finally
        {
            _isInitialLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        await _settingsService.SetSettingAsync("General.EnableMicaAlt", EnableMicaAlt, cancellationToken);
        await _settingsService.SetThemeAsync(SelectedTheme, cancellationToken);
        if (SelectedLanguage is not null)
        {
            await _localizationService.SetLanguageAsync(SelectedLanguage.Language, cancellationToken);
        }

        await _updateService.SetChannelAsync(SelectedChannel, cancellationToken);
        await _updateService.SetAutoCheckOnStartupAsync(AutoCheckOnStartup, cancellationToken);

        SetStatusNotice(_localizationService.GetString(LocalizationKeys.Settings_SavedSuccess));
    }

    // --- Update Commands ---

    [RelayCommand]
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        if (IsCheckingUpdates || IsDownloadingUpdate) return;

        try
        {
            IsCheckingUpdates = true;
            UpdateErrorMessage = string.Empty;
            CurrentUpdateState = UpdateState.Checking;

            var result = await _updateService.CheckForUpdatesAsync(SelectedChannel, cancellationToken);
            UpdateLastCheckedDisplay();

            if (!result.IsSuccess)
            {
                CurrentUpdateState = UpdateState.Error;
                UpdateErrorMessage = result.Error.Code == "Update.NetworkError"
                    ? _localizationService.GetString(LocalizationKeys.Settings_Updates_ErrorNetwork)
                    : _localizationService.GetFormattedString(LocalizationKeys.Settings_Updates_ErrorGeneric, result.Error.Message);
                return;
            }

            var checkResult = result.Value;
            if (checkResult.IsUpdateAvailable && checkResult.LatestRelease is not null)
            {
                _latestUpdateInfo = checkResult.LatestRelease;
                LatestVersionText = _latestUpdateInfo.Version;
                LatestReleaseTitle = _latestUpdateInfo.DisplayName;
                ReleaseNotesText = _latestUpdateInfo.ReleaseNotes;
                DownloadSizeText = _latestUpdateInfo.FileSizeBytes > 0
                    ? ByteSizeFormatter.FormatBytes(_latestUpdateInfo.FileSizeBytes)
                    : string.Empty;

                UpdatesAvailableText = _localizationService.GetFormattedString(LocalizationKeys.Settings_Updates_Available, LatestVersionText);

                IsUpdateAvailable = true;
                IsUpToDate = false;
                CurrentUpdateState = UpdateState.UpdateAvailable;
            }
            else
            {
                _latestUpdateInfo = null;
                IsUpdateAvailable = false;
                IsUpToDate = true;
                CurrentUpdateState = UpdateState.UpToDate;

                _upToDateCts?.Cancel();
                _upToDateCts?.Dispose();
                _upToDateCts = new CancellationTokenSource();
                var upToDateToken = _upToDateCts.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000, upToDateToken);
                        if (!upToDateToken.IsCancellationRequested)
                        {
                            _dispatcherService.TryEnqueue(() =>
                            {
                                if (!upToDateToken.IsCancellationRequested && IsUpToDate)
                                {
                                    IsUpToDate = false;
                                    if (CurrentUpdateState == UpdateState.UpToDate)
                                    {
                                        CurrentUpdateState = UpdateState.Idle;
                                    }
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }, upToDateToken);
            }
        }
        catch (OperationCanceledException)
        {
            CurrentUpdateState = UpdateState.Idle;
        }
        catch (Exception ex)
        {
            CurrentUpdateState = UpdateState.Error;
            UpdateErrorMessage = ex.Message;
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    public async Task DownloadUpdateAsync()
    {
        if (_latestUpdateInfo is null || IsDownloadingUpdate) return;

        _downloadCts = new CancellationTokenSource();
        IsDownloadingUpdate = true;
        CurrentUpdateState = UpdateState.Downloading;
        UpdateErrorMessage = string.Empty;
        DownloadProgressPercentage = 0;
        DownloadProgressText = string.Empty;
        DownloadSpeedText = string.Empty;

        var progress = new Progress<UpdateProgressInfo>(info =>
        {
            _dispatcherService.TryEnqueue(() =>
            {
                DownloadProgressPercentage = info.Percentage;
                var rec = ByteSizeFormatter.FormatBytes(info.BytesReceived);
                var tot = info.TotalBytesToReceive > 0 ? ByteSizeFormatter.FormatBytes(info.TotalBytesToReceive) : "?";
                DownloadProgressText = $"{info.Percentage:0.#}% ({rec} / {tot})";

                if (info.BytesPerSecond > 0)
                {
                    DownloadSpeedText = $"{ByteSizeFormatter.FormatBytes((long)info.BytesPerSecond)}/s";
                }
            });
        });

        try
        {
            var result = await _updateService.DownloadUpdateAsync(_latestUpdateInfo, progress, _downloadCts.Token);

            if (!result.IsSuccess)
            {
                if (result.Error.Code == "Common.Cancelled")
                {
                    CurrentUpdateState = UpdateState.UpdateAvailable;
                    StatusNotice = _localizationService.GetString(LocalizationKeys.Settings_Updates_DownloadCancelled);
                }
                else if (result.Error.Code == "Security.HashMismatch")
                {
                    CurrentUpdateState = UpdateState.Error;
                    UpdateErrorMessage = _localizationService.GetString(LocalizationKeys.Settings_Updates_ErrorHashMismatch);
                }
                else
                {
                    CurrentUpdateState = UpdateState.Error;
                    UpdateErrorMessage = _localizationService.GetFormattedString(LocalizationKeys.Settings_Updates_ErrorGeneric, result.Error.Message);
                }
                return;
            }

            DownloadedInstallerPath = result.Value;
            IsReadyToInstall = true;
            IsUpdateAvailable = false;
            CurrentUpdateState = UpdateState.ReadyToInstall;
        }
        catch (OperationCanceledException)
        {
            CurrentUpdateState = UpdateState.UpdateAvailable;
            StatusNotice = _localizationService.GetString(LocalizationKeys.Settings_Updates_DownloadCancelled);
        }
        catch (Exception ex)
        {
            CurrentUpdateState = UpdateState.Error;
            UpdateErrorMessage = ex.Message;
        }
        finally
        {
            IsDownloadingUpdate = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    [RelayCommand]
    public void CancelDownload()
    {
        _downloadCts?.Cancel();
    }

    [RelayCommand]
    public async Task InstallUpdateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DownloadedInstallerPath)) return;

        try
        {
            var result = await _updateService.LaunchInstallerAsync(DownloadedInstallerPath, cancellationToken);
            if (!result.IsSuccess)
            {
                UpdateErrorMessage = result.Error.Message;
                CurrentUpdateState = UpdateState.Error;
            }
        }
        catch (Exception ex)
        {
            UpdateErrorMessage = ex.Message;
            CurrentUpdateState = UpdateState.Error;
        }
    }

    [RelayCommand]
    public void DismissUpdateError()
    {
        UpdateErrorMessage = string.Empty;
        if (CurrentUpdateState == UpdateState.Error)
        {
            CurrentUpdateState = _latestUpdateInfo is not null ? UpdateState.UpdateAvailable : UpdateState.Idle;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _localizationService.LanguageChanged -= OnLanguageChanged;
                _downloadCts?.Cancel();
                _downloadCts?.Dispose();
                _downloadCts = null;
                _statusNoticeCts?.Cancel();
                _statusNoticeCts?.Dispose();
                _statusNoticeCts = null;
                _upToDateCts?.Cancel();
                _upToDateCts?.Dispose();
                _upToDateCts = null;
            }
            _disposed = true;
        }
    }
}
