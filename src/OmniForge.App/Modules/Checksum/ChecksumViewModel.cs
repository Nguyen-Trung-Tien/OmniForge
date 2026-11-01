using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Core.Modules.Checksum;

namespace OmniForge.App.Modules.Checksum;

public partial class ChecksumViewModel : ToolViewModelBase
{
    private readonly IChecksumService _checksumService;
    private readonly IFilePickerService? _filePickerService;
    private readonly IClipboardService? _clipboardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompute))]
    public partial string TargetFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SupportedHashAlgorithm SelectedAlgorithm { get; set; } = SupportedHashAlgorithm.Sha256;

    public IReadOnlyList<SupportedHashAlgorithm> AvailableAlgorithms { get; } =
    [
        SupportedHashAlgorithm.Md5,
        SupportedHashAlgorithm.Sha1,
        SupportedHashAlgorithm.Sha256,
        SupportedHashAlgorithm.Sha384,
        SupportedHashAlgorithm.Sha512
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasComputedHash))]
    public partial string ComputedHash { get; set; } = string.Empty;

    public bool HasComputedHash => !string.IsNullOrEmpty(ComputedHash);

    [ObservableProperty]
    public partial string FormattedFileSize { get; set; } = "—";

    [ObservableProperty]
    public partial string ExpectedHash { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasComparison))]
    public partial string ComparisonStatusText { get; set; } = string.Empty;

    public bool HasComparison => !string.IsNullOrEmpty(ComparisonStatusText);

    [ObservableProperty]
    public partial bool IsHashMatch { get; set; }

    public bool CanCompute => !string.IsNullOrWhiteSpace(TargetFilePath) && IsNotBusy;

    public ChecksumViewModel(
        IChecksumService checksumService,
        IFilePickerService? filePickerService = null,
        IClipboardService? clipboardService = null)
    {
        _checksumService = checksumService ?? throw new ArgumentNullException(nameof(checksumService));
        _filePickerService = filePickerService;
        _clipboardService = clipboardService;
    }

    [RelayCommand]
    private async Task BrowseFileAsync(CancellationToken cancellationToken)
    {
        if (_filePickerService is null) return;

        var result = await _filePickerService.PickSingleFileAsync(cancellationToken: cancellationToken);
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
        {
            TargetFilePath = result.Value;
            await ComputeHashAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task ComputeHashAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(TargetFilePath))
        {
            ErrorMessage = "Please select or enter a valid file path.";
            return;
        }

        var pathValidation = SecurityGuard.ValidateSafePath(TargetFilePath);
        if (pathValidation.IsFailure)
        {
            ErrorMessage = pathValidation.Error.Message;
            return;
        }

        ComparisonStatusText = string.Empty;

        var progress = new Progress<double>(p =>
        {
            ReportProgress((int)p, $"Computing {SelectedAlgorithm} hash... ({p:F0}%)");
        });

        var result = await RunSafeAsync(
            ct => _checksumService.ComputeFileHashAsync(pathValidation.Value, SelectedAlgorithm, progress, ct),
            busyStatus: $"Hashing file ({SelectedAlgorithm})...",
            successStatus: "Hash computation completed.",
            cancellationToken: cancellationToken);

        if (result is not null)
        {
            ComputedHash = result.HexDigest;
            FormattedFileSize = ByteSizeFormatter.FormatBytes(result.BytesProcessed);

            if (!string.IsNullOrWhiteSpace(ExpectedHash))
            {
                VerifyHashComparison();
            }
        }
    }

    [RelayCommand]
    private void VerifyHashComparison()
    {
        if (string.IsNullOrWhiteSpace(ComputedHash))
        {
            ComparisonStatusText = "Please compute the file hash first.";
            IsHashMatch = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ExpectedHash))
        {
            ComparisonStatusText = "Please enter an expected hash to compare.";
            IsHashMatch = false;
            return;
        }

        var comparison = _checksumService.CompareHashes(ComputedHash, ExpectedHash);
        IsHashMatch = comparison.IsMatch;
        ComparisonStatusText = comparison.Details;
    }

    [RelayCommand]
    private void CopyComputedHash()
    {
        if (_clipboardService is null || string.IsNullOrWhiteSpace(ComputedHash)) return;

        var res = _clipboardService.CopyText(ComputedHash);
        StatusMessage = res.IsSuccess ? "Checksum copied to clipboard." : res.Error.Message;
    }

    [RelayCommand]
    private void ClearAll()
    {
        TargetFilePath = string.Empty;
        ComputedHash = string.Empty;
        ExpectedHash = string.Empty;
        ComparisonStatusText = string.Empty;
        FormattedFileSize = "—";
        IsHashMatch = false;
        StatusMessage = "Cleared.";
    }

    partial void OnTargetFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(CanCompute));
    }
}

