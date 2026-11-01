using System.Security.Cryptography;
using System.Text;
using OmniForge.Core.Modules.Checksum;
using Xunit;

namespace OmniForge.Tests.Core;

public class StreamHashEngineTests
{
    [Fact]
    public async Task ComputeStreamHashAsync_Sha256_ShouldMatchKnownVector()
    {
        // "Hello, OmniForge!" SHA-256
        byte[] data = Encoding.UTF8.GetBytes("Hello, OmniForge!");
        using var stream = new MemoryStream(data);

        var result = await StreamHashEngine.ComputeStreamHashAsync(stream, SupportedHashAlgorithm.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal("SHA256", result.Value.Algorithm);
        Assert.Equal(data.Length, result.Value.BytesProcessed);
        Assert.NotEmpty(result.Value.HexDigest);

        // Verify with .NET standard SHA256
        byte[] expectedHash = SHA256.HashData(data);
        string expectedHex = Convert.ToHexStringLower(expectedHash);
        Assert.Equal(expectedHex, result.Value.HexDigest);
    }

    [Theory]
    [InlineData(SupportedHashAlgorithm.Md5)]
    [InlineData(SupportedHashAlgorithm.Sha1)]
    [InlineData(SupportedHashAlgorithm.Sha256)]
    [InlineData(SupportedHashAlgorithm.Sha384)]
    [InlineData(SupportedHashAlgorithm.Sha512)]
    public async Task ComputeStreamHashAsync_AllAlgorithms_ShouldProduceValidHex(SupportedHashAlgorithm algorithm)
    {
        byte[] data = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
        using var stream = new MemoryStream(data);

        var result = await StreamHashEngine.ComputeStreamHashAsync(stream, algorithm);

        Assert.True(result.IsSuccess);
        Assert.Equal(data.Length, result.Value.BytesProcessed);
        Assert.NotEmpty(result.Value.HexDigest);
    }

    [Fact]
    public async Task ComputeStreamHashAsync_LargeStream_ShouldProcessCorrectBytes()
    {
        // 150 KB (> two 64 KB buffers)
        byte[] largeData = new byte[150 * 1024];
        new Random(42).NextBytes(largeData);
        using var stream = new MemoryStream(largeData);

        var result = await StreamHashEngine.ComputeStreamHashAsync(stream, SupportedHashAlgorithm.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal(largeData.Length, result.Value.BytesProcessed);

        byte[] expectedHash = SHA256.HashData(largeData);
        string expectedHex = Convert.ToHexStringLower(expectedHash);
        Assert.Equal(expectedHex, result.Value.HexDigest);
    }

    [Fact]
    public async Task ComputeStreamHashAsync_EmptyStream_ShouldMatchEmptyHash()
    {
        using var stream = new MemoryStream([]);

        var result = await StreamHashEngine.ComputeStreamHashAsync(stream, SupportedHashAlgorithm.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.BytesProcessed);

        // Standard SHA256 of empty string is e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", result.Value.HexDigest);
    }

    [Fact]
    public void CompareHashes_MatchingDigests_ShouldReturnTrue()
    {
        string hashA = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
        string hashB = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        var comp = StreamHashEngine.CompareHashes(hashA, hashB);

        Assert.True(comp.IsMatch);
        Assert.Contains("matches", comp.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompareHashes_MismatchedDigests_ShouldReturnFalse()
    {
        string hashA = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        string hashB = "0000000000000000000000000000000000000000000000000000000000000000";

        var comp = StreamHashEngine.CompareHashes(hashA, hashB);

        Assert.False(comp.IsMatch);
        Assert.Contains("mismatch", comp.Details, StringComparison.OrdinalIgnoreCase);
    }
}

