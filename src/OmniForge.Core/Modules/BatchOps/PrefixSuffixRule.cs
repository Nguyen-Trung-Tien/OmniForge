using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Modules.BatchOps;

/// <summary>
/// Prepends a prefix and/or appends a suffix to the file name.
/// </summary>
public sealed class PrefixSuffixRule : IRenameRule
{
    public string Name => "Prefix / Suffix";

    public bool IsEnabled { get; set; } = true;

    public string Prefix { get; set; } = string.Empty;

    public string Suffix { get; set; } = string.Empty;

    public PrefixSuffixRule()
    {
    }

    public PrefixSuffixRule(string prefix, string suffix)
    {
        Prefix = prefix ?? string.Empty;
        Suffix = suffix ?? string.Empty;
    }

    public string Apply(string originalName, int index, FileMetadata metadata)
    {
        if (!IsEnabled)
        {
            return originalName;
        }

        return $"{Prefix}{originalName}{Suffix}";
    }
}

