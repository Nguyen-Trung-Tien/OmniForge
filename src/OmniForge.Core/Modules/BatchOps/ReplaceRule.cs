using System.Globalization;
using System.Text.RegularExpressions;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Modules.BatchOps;

/// <summary>
/// Searches and replaces substrings within the file name, supporting plain text, case matching,
/// ReDoS-protected regular expressions, and contextual token interpolation.
/// </summary>
public sealed class ReplaceRule : IRenameRule
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public string Name => "Search & Replace";

    public bool IsEnabled { get; set; } = true;

    public string SearchPattern { get; set; } = string.Empty;

    public string Replacement { get; set; } = string.Empty;

    public bool MatchCase { get; set; }

    public bool UseRegex { get; set; }

    public ReplaceRule()
    {
    }

    public ReplaceRule(string searchPattern, string replacement, bool matchCase = false, bool useRegex = false)
    {
        SearchPattern = searchPattern ?? string.Empty;
        Replacement = replacement ?? string.Empty;
        MatchCase = matchCase;
        UseRegex = useRegex;
    }

    public string Apply(string originalName, int index, FileMetadata metadata)
    {
        if (!IsEnabled || string.IsNullOrEmpty(SearchPattern))
        {
            return originalName;
        }

        string evaluatedReplacement = InterpolateTokens(Replacement, index, metadata);

        if (UseRegex)
        {
            try
            {
                var options = MatchCase
                    ? RegexOptions.CultureInvariant
                    : RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

                return Regex.Replace(originalName, SearchPattern, evaluatedReplacement, options, RegexTimeout);
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern supplied by user: gracefully return unchanged
                return originalName;
            }
            catch (RegexMatchTimeoutException)
            {
                // Catastrophic backtracking ReDoS protection
                return originalName;
            }
        }

        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return originalName.Replace(SearchPattern, evaluatedReplacement, comparison);
    }

    /// <summary>
    /// Interpolates standard token placeholders such as {index}, {date}, {parent}, and {ext}.
    /// </summary>
    public static string InterpolateTokens(string text, int index, FileMetadata metadata)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        string parentFolder = !string.IsNullOrEmpty(metadata.DirectoryName)
            ? Path.GetFileName(metadata.DirectoryName)
            : string.Empty;

        return text
            .Replace("{index}", (index + 1).ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", metadata.ModifiedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{parent}", parentFolder, StringComparison.OrdinalIgnoreCase)
            .Replace("{ext}", metadata.Extension.TrimStart('.'), StringComparison.OrdinalIgnoreCase);
    }
}

