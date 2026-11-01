using System.Globalization;
using OmniForge.Core.Contracts;
using OmniForge.Core.Models;

namespace OmniForge.Modules.BatchOps;

/// <summary>
/// Specifies the insertion position for sequential numbering.
/// </summary>
public enum NumberingPosition
{
    Suffix,
    Prefix
}

/// <summary>
/// Appends or prepends sequential numbers with customizable start index, step increment, and padding format (e.g. 001).
/// Also replaces the '{num}' token anywhere in the file name if present.
/// </summary>
public sealed class NumberingRule : IRenameRule
{
    public string Name => "Numbering";

    public bool IsEnabled { get; set; } = true;

    public int StartNumber { get; set; } = 1;

    public int Step { get; set; } = 1;

    public string PaddingFormat { get; set; } = "001";

    public NumberingPosition Position { get; set; } = NumberingPosition.Suffix;

    public string Separator { get; set; } = "_";

    public NumberingRule()
    {
    }

    public NumberingRule(
        int startNumber = 1,
        int step = 1,
        string paddingFormat = "001",
        NumberingPosition position = NumberingPosition.Suffix,
        string separator = "_")
    {
        StartNumber = startNumber;
        Step = step;
        PaddingFormat = paddingFormat ?? "001";
        Position = position;
        Separator = separator ?? "_";
    }

    public string Apply(string originalName, int index, FileMetadata metadata)
    {
        if (!IsEnabled)
        {
            return originalName;
        }

        int currentNumber = StartNumber + (index * Step);
        string formattedNumber = FormatNumber(currentNumber, PaddingFormat);

        // If the name explicitly contains '{num}', replace that placeholder directly
        if (originalName.Contains("{num}", StringComparison.OrdinalIgnoreCase))
        {
            return originalName.Replace("{num}", formattedNumber, StringComparison.OrdinalIgnoreCase);
        }

        return Position switch
        {
            NumberingPosition.Prefix => $"{formattedNumber}{Separator}{originalName}",
            _ => $"{originalName}{Separator}{formattedNumber}"
        };
    }

    private static string FormatNumber(int number, string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        // If user gave a template like "001" or "0000", convert to D-specifier
        if (format.All(char.IsDigit))
        {
            return number.ToString($"D{format.Length}", CultureInfo.InvariantCulture);
        }

        try
        {
            return number.ToString(format, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }
    }
}

