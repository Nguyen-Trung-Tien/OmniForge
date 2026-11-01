namespace OmniForge.Core.Models;

/// <summary>
/// Immutable progress report snapshot emitted during chunked or batch file processing.
/// </summary>
public sealed record BatchProgressReport(
    int TotalItems,
    int ProcessedItems,
    int SucceededItems,
    int FailedItems,
    string CurrentItemName,
    double Percentage,
    TimeSpan ElapsedTime,
    TimeSpan EstimatedTimeRemaining)
{
    /// <summary>
    /// Creates an empty initial progress report.
    /// </summary>
    public static BatchProgressReport Empty => new(
        TotalItems: 0,
        ProcessedItems: 0,
        SucceededItems: 0,
        FailedItems: 0,
        CurrentItemName: string.Empty,
        Percentage: 0.0,
        ElapsedTime: TimeSpan.Zero,
        EstimatedTimeRemaining: TimeSpan.Zero);
}

