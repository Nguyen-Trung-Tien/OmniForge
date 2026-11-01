namespace OmniForge.Core.Modules.LuckyWheel;

/// <summary>
/// Data model representing a single option on the Lucky Wheel.
/// Fully UI-agnostic to comply with Clean Architecture rules.
/// </summary>
public sealed class WheelItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public double Weight { get; set; } = 1.0;

    public string Color { get; set; } = "#2563EB";

    public bool IsEnabled { get; set; } = true;

    public WheelItem() { }

    public WheelItem(string text, double weight = 1.0, string color = "#2563EB", bool isEnabled = true)
    {
        Text = text;
        Weight = Math.Max(0.0, weight);
        Color = color;
        IsEnabled = isEnabled;
    }

    public WheelItem Clone() => new()
    {
        Id = Guid.NewGuid(),
        Text = Text,
        Weight = Weight,
        Color = Color,
        IsEnabled = IsEnabled
    };
}

/// <summary>
/// Pre-calculated angular geometry for rendering a wheel segment.
/// </summary>
public sealed record WheelSlice(
    WheelItem Item,
    int Index,
    double StartAngle,
    double EndAngle,
    double SweepAngle,
    double MidAngle,
    double Percentage);

/// <summary>
/// Physics deceleration profile for wheel spin animation.
/// </summary>
public enum DecelerationProfile
{
    EaseOutCubic,
    Exponential
}

/// <summary>
/// Result snapshot produced when a spin completes.
/// </summary>
public sealed record SpinTarget(
    WheelItem WinningItem,
    int WinningIndex,
    double TargetAngle,
    int RotationsCount,
    double DurationSeconds);
