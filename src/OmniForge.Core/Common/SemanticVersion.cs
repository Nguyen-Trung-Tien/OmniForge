using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OmniForge.Core.Common;

/// <summary>
/// Immutable, production-grade Semantic Version (SemVer 2.0.0) parser and comparer.
/// Handles standard versioning (Major.Minor.Patch), optional pre-release identifiers, and build metadata.
/// </summary>
public sealed partial class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>, IComparable
{
    private static readonly Regex SemVerRegex = CreateSemVerRegex();

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<string> PreReleaseIdentifiers { get; }
    public string? BuildMetadata { get; }
    public bool IsPreRelease => PreReleaseIdentifiers.Count > 0;

    public SemanticVersion(
        int major,
        int minor,
        int patch,
        IEnumerable<string>? preReleaseIdentifiers = null,
        string? buildMetadata = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(major, nameof(major));
        ArgumentOutOfRangeException.ThrowIfNegative(minor, nameof(minor));
        ArgumentOutOfRangeException.ThrowIfNegative(patch, nameof(patch));

        Major = major;
        Minor = minor;
        Patch = patch;
        PreReleaseIdentifiers = preReleaseIdentifiers?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)[];
        BuildMetadata = string.IsNullOrWhiteSpace(buildMetadata) ? null : buildMetadata.Trim();
    }

    /// <summary>
    /// Parses a semantic version string (e.g., "1.2.3", "v0.1.0-alpha", "2.0.0-beta.1+2024").
    /// </summary>
    public static bool TryParse(string? versionString, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(versionString))
        {
            return false;
        }

        var trimmed = versionString.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..].Trim();
        }

        var match = SemVerRegex.Match(trimmed);
        if (!match.Success)
        {
            // Fallback for simple .NET System.Version format like "1.0" or "1.0.0.0"
            if (Version.TryParse(trimmed, out var netVer))
            {
                version = new SemanticVersion(
                    Math.Max(0, netVer.Major),
                    Math.Max(0, netVer.Minor),
                    Math.Max(0, netVer.Build));
                return true;
            }

            return false;
        }

        if (!int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major) ||
            !int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor) ||
            !int.TryParse(match.Groups["patch"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var patch))
        {
            return false;
        }

        var preReleaseGroup = match.Groups["prerelease"];
        var preReleaseList = preReleaseGroup.Success && !string.IsNullOrWhiteSpace(preReleaseGroup.Value)
            ? preReleaseGroup.Value.Split('.', StringSplitOptions.RemoveEmptyEntries)
            : [];

        var buildGroup = match.Groups["build"];
        var buildMetadata = buildGroup.Success ? buildGroup.Value : null;

        version = new SemanticVersion(major, minor, patch, preReleaseList, buildMetadata);
        return true;
    }

    public static SemanticVersion Parse(string versionString)
    {
        if (!TryParse(versionString, out var version))
        {
            throw new FormatException($"Invalid semantic version string: '{versionString}'.");
        }

        return version;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        int majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        int minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;

        int patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0) return patchCompare;

        // SemVer 2.0.0: Normal version has higher precedence than pre-release version
        if (!IsPreRelease && other.IsPreRelease) return 1;
        if (IsPreRelease && !other.IsPreRelease) return -1;
        if (!IsPreRelease && !other.IsPreRelease) return 0;

        // Both are pre-releases: compare identifier by identifier
        int minLength = Math.Min(PreReleaseIdentifiers.Count, other.PreReleaseIdentifiers.Count);
        for (int i = 0; i < minLength; i++)
        {
            var idA = PreReleaseIdentifiers[i];
            var idB = other.PreReleaseIdentifiers[i];

            bool isNumA = int.TryParse(idA, NumberStyles.None, CultureInfo.InvariantCulture, out var numA);
            bool isNumB = int.TryParse(idB, NumberStyles.None, CultureInfo.InvariantCulture, out var numB);

            if (isNumA && isNumB)
            {
                int numComp = numA.CompareTo(numB);
                if (numComp != 0) return numComp;
            }
            else if (isNumA)
            {
                // Numeric identifier has lower precedence than string identifier
                return -1;
            }
            else if (isNumB)
            {
                return 1;
            }
            else
            {
                int stringComp = string.Compare(idA, idB, StringComparison.OrdinalIgnoreCase);
                if (stringComp != 0) return stringComp;
            }
        }

        return PreReleaseIdentifiers.Count.CompareTo(other.PreReleaseIdentifiers.Count);
    }

    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            SemanticVersion other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(SemanticVersion)}.", nameof(obj))
        };

    public bool Equals(SemanticVersion? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               PreReleaseIdentifiers.SequenceEqual(other.PreReleaseIdentifiers, StringComparer.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Major);
        hash.Add(Minor);
        hash.Add(Patch);
        foreach (var id in PreReleaseIdentifiers)
        {
            hash.Add(id, StringComparer.OrdinalIgnoreCase);
        }
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var baseVersion = $"{Major}.{Minor}.{Patch}";
        if (IsPreRelease)
        {
            baseVersion += $"-{string.Join('.', PreReleaseIdentifiers)}";
        }
        if (!string.IsNullOrEmpty(BuildMetadata))
        {
            baseVersion += $"+{BuildMetadata}";
        }
        return baseVersion;
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => !(left == right);

    public static bool operator <(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(SemanticVersion? left, SemanticVersion? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;

    [GeneratedRegex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex CreateSemVerRegex();
}

