using OmniForge.Core.Common;

namespace OmniForge.Tests.Core;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0, false, null)]
    [InlineData("v2.15.3", 2, 15, 3, false, null)]
    [InlineData("0.1.0-alpha", 0, 1, 0, true, "alpha")]
    [InlineData("v1.2.3-beta.2", 1, 2, 3, true, "beta.2")]
    [InlineData("3.0.0-rc.1+build.2026", 3, 0, 0, true, "rc.1")]
    public void TryParse_ValidSemVerStrings_ShouldParseCorrectly(
        string input, int expectedMajor, int expectedMinor, int expectedPatch, bool isPreRelease, string? expectedPreRelease)
    {
        var success = SemanticVersion.TryParse(input, out var version);

        Assert.True(success);
        Assert.NotNull(version);
        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(expectedMinor, version.Minor);
        Assert.Equal(expectedPatch, version.Patch);
        Assert.Equal(isPreRelease, version.IsPreRelease);

        if (expectedPreRelease != null)
        {
            Assert.Equal(expectedPreRelease, string.Join('.', version.PreReleaseIdentifiers));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("1.0.0.0.0")]
    [InlineData("-1.0.0")]
    public void TryParse_InvalidStrings_ShouldReturnFalse(string? input)
    {
        var success = SemanticVersion.TryParse(input, out var version);

        Assert.False(success);
        Assert.Null(version);
    }

    [Fact]
    public void CompareTo_StandardPrecedence_ShouldOrderCorrectly()
    {
        var v010Alpha = SemanticVersion.Parse("0.1.0-alpha");
        var v010Beta = SemanticVersion.Parse("0.1.0-beta");
        var v010 = SemanticVersion.Parse("0.1.0");
        var v020 = SemanticVersion.Parse("0.2.0");
        var v100 = SemanticVersion.Parse("1.0.0");

        Assert.True(v010Alpha < v010Beta);
        Assert.True(v010Beta < v010);
        Assert.True(v010 < v020);
        Assert.True(v020 < v100);
        Assert.True(v100 > v010);
    }

    [Fact]
    public void CompareTo_PreReleaseNumericComparison_ShouldTreatNumbersNumerically()
    {
        var v1 = SemanticVersion.Parse("1.0.0-alpha.2");
        var v2 = SemanticVersion.Parse("1.0.0-alpha.10");

        // "2" < "10" numerically (SemVer 2.0.0 rule 11.4)
        Assert.True(v1 < v2);
    }

    [Fact]
    public void CompareTo_BuildMetadata_ShouldBeIgnoredInPrecedence()
    {
        var v1 = SemanticVersion.Parse("1.0.0+build1");
        var v2 = SemanticVersion.Parse("1.0.0+build2");

        Assert.Equal(0, v1.CompareTo(v2));
        Assert.True(v1 == v2);
    }

    [Fact]
    public void Equals_SameVersions_ShouldReturnTrueAndMatchingHashCode()
    {
        var v1 = SemanticVersion.Parse("v1.2.3-beta.1");
        var v2 = SemanticVersion.Parse("1.2.3-beta.1");

        Assert.Equal(v1, v2);
        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }
}

