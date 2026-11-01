using OmniForge.Core.Common;
using OmniForge.Core.Modules.Checksum;

namespace OmniForge.App.Modules.Checksum;

public interface IChecksumService
{
    Task<Result<FileHashResult>> ComputeFileHashAsync(
        string filePath,
        SupportedHashAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<Result<FileHashResult>> ComputeStreamHashAsync(
        Stream stream,
        SupportedHashAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    ChecksumComparisonResult CompareHashes(string computedHash, string expectedHash);
}

public sealed class ChecksumService : IChecksumService
{
    public Task<Result<FileHashResult>> ComputeFileHashAsync(
        string filePath,
        SupportedHashAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        StreamHashEngine.ComputeFileHashAsync(filePath, algorithm, progress, cancellationToken);

    public Task<Result<FileHashResult>> ComputeStreamHashAsync(
        Stream stream,
        SupportedHashAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        StreamHashEngine.ComputeStreamHashAsync(stream, algorithm, progress, cancellationToken);

    public ChecksumComparisonResult CompareHashes(string computedHash, string expectedHash) =>
        StreamHashEngine.CompareHashes(computedHash, expectedHash);
}

