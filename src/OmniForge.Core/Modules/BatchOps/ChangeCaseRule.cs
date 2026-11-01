using System.Globalization;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Modules.BatchOps;

/// <summary>
/// Specifies the target casing mode for text transformation.
/// </summary>
public enum CaseMode
{
    Lowercase,
    Uppercase,
    TitleCase
}

/// <summary>
/// Transforms the file name letter casing (lowercase, UPPERCASE, Title Case).
/// </summary>
public sealed class ChangeCaseRule : IRenameRule
{
    public string Name => "Change Case";

    public bool IsEnabled { get; set; } = true;

    public CaseMode Mode { get; set; } = CaseMode.Lowercase;

    public ChangeCaseRule()
    {
    }

    public ChangeCaseRule(CaseMode mode)
    {
        Mode = mode;
    }

    public string Apply(string originalName, int index, FileMetadata metadata)
    {
        if (!IsEnabled || string.IsNullOrEmpty(originalName))
        {
            return originalName;
        }

        return Mode switch
        {
            CaseMode.Lowercase => originalName.ToLowerInvariant(),
            CaseMode.Uppercase => originalName.ToUpperInvariant(),
            CaseMode.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(originalName.ToLower(CultureInfo.CurrentCulture)),
            _ => originalName
        };
    }
}

