using System.Globalization;
using System.Text.RegularExpressions;

namespace OmniForge.Core.Modules.FolderCreator;

/// <summary>
/// Dynamic variable token parser and evaluator for batch path interpolation.
/// Supports tokens: {date:format}, {time:format}, {guid:format}, and {seq:width,start}.
/// Protected against ReDoS via compiled source-generated regex with execution timeouts.
/// </summary>
public static partial class TokenEvaluator
{
    private const string DefaultDateFormat = "yyyy-MM-dd";
    private const string DefaultTimeFormat = "HHmmss";

    [GeneratedRegex(@"\{(?<type>date|time|guid|seq)(?::(?<spec>[^}]+))?\}", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TokenPattern();

    /// <summary>
    /// Evaluates dynamic tokens within <paramref name="template"/> substituting date, time, guid, and sequence values.
    /// </summary>
    /// <param name="template">String template containing token placeholders.</param>
    /// <param name="sequenceNumber">Current sequence counter value.</param>
    /// <param name="referenceTime">Optional fixed reference time for deterministic testing.</param>
    /// <returns>Interpolated string with all tokens resolved.</returns>
    public static string Evaluate(string? template, int sequenceNumber, DateTime? referenceTime = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var now = referenceTime ?? DateTime.Now;

        return TokenPattern().Replace(template, match =>
        {
            var type = match.Groups["type"].Value;
            var spec = match.Groups["spec"].Success ? match.Groups["spec"].Value.Trim() : null;

            return type.ToLowerInvariant() switch
            {
                "date" => FormatDate(now, spec),
                "time" => FormatTime(now, spec),
                "guid" => FormatGuid(spec),
                "seq"  => FormatSequence(sequenceNumber, spec),
                _      => match.Value
            };
        });
    }

    /// <summary>
    /// Checks whether a template string contains any dynamic tokens.
    /// </summary>
    public static bool HasTokens(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return false;
        }

        return TokenPattern().IsMatch(template);
    }

    /// <summary>
    /// Checks whether a template string contains a sequence token ({seq} or {seq:...}).
    /// </summary>
    public static bool HasSequenceToken(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return false;
        }

        return template.Contains("{seq", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDate(DateTime now, string? format)
    {
        var fmt = string.IsNullOrWhiteSpace(format) ? DefaultDateFormat : format;
        try
        {
            return now.ToString(fmt, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return now.ToString(DefaultDateFormat, CultureInfo.InvariantCulture);
        }
    }

    private static string FormatTime(DateTime now, string? format)
    {
        var fmt = string.IsNullOrWhiteSpace(format) ? DefaultTimeFormat : format;
        try
        {
            return now.ToString(fmt, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return now.ToString(DefaultTimeFormat, CultureInfo.InvariantCulture);
        }
    }

    private static string FormatGuid(string? format)
    {
        var fmt = string.IsNullOrWhiteSpace(format) ? "D" : format;
        try
        {
            return Guid.NewGuid().ToString(fmt, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
        }
    }

    private static string FormatSequence(int sequenceNumber, string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return sequenceNumber.ToString(CultureInfo.InvariantCulture);
        }

        // Support {seq:width} and {seq:width,start}
        int width = 1;
        int startOffset = 0;

        if (spec.Contains(','))
        {
            var parts = spec.Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedWidth))
            {
                width = Math.Max(1, Math.Min(parsedWidth, 20));
            }
            if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStart))
            {
                startOffset = parsedStart - 1;
            }
        }
        else if (int.TryParse(spec, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedWidth))
        {
            width = Math.Max(1, Math.Min(parsedWidth, 20));
        }

        long effectiveValue = (long)sequenceNumber + startOffset;
        return effectiveValue.ToString($"D{width}", CultureInfo.InvariantCulture);
    }
}

