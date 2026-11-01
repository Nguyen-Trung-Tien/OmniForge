using System.Globalization;

namespace OmniForge.Core.Common;

/// <summary>
/// Standardized, culture-invariant formatting utilities for file sizes, throughput, and execution durations.
/// Shared across all OmniForge modules and ViewModels.
/// </summary>
public static class ByteSizeFormatter
{
    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>
    /// Formats a byte count into a culture-invariant human-readable string (e.g., "1.5 MB").
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";

        int mag = (int)Math.Log(bytes, 1024);
        mag = Math.Min(mag, SizeSuffixes.Length - 1);
        double adjustedSize = bytes / Math.Pow(1024, mag);

        return mag == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{bytes} B")
            : string.Create(CultureInfo.InvariantCulture, $"{adjustedSize:0.##} {SizeSuffixes[mag]}");
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> into a compact culture-invariant duration string (e.g., "14 ms", "1.25 s").
    /// </summary>
    public static string FormatDuration(TimeSpan elapsed) =>
        elapsed.TotalMilliseconds switch
        {
            < 1.0 => "< 1 ms",
            < 1000.0 => string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalMilliseconds:0} ms"),
            < 60_000.0 => string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalSeconds:0.00} s"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{elapsed.Minutes}m {elapsed.Seconds}s")
        };
}
