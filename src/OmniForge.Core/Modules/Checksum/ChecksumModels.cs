using System.Security.Cryptography;

namespace OmniForge.Core.Modules.Checksum;

public enum SupportedHashAlgorithm
{
    Md5,
    Sha1,
    Sha256,
    Sha384,
    Sha512
}

public sealed record FileHashResult(
    string Algorithm,
    string HexDigest,
    long BytesProcessed,
    TimeSpan Duration);

public sealed record ChecksumComparisonResult(
    bool IsMatch,
    string ComputedHash,
    string ExpectedHash,
    string Details);

