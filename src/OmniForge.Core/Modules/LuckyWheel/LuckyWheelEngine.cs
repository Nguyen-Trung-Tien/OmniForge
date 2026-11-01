namespace OmniForge.Core.Modules.LuckyWheel;

/// <summary>
/// High-performance, deterministic physics and geometry calculation engine for the Lucky Wheel.
/// Fully UI-agnostic and thread-safe.
/// </summary>
public static class LuckyWheelEngine
{
    public const double DefaultPointerAngle = -Math.PI / 2.0; // 12 o'clock (top center)
    public const double TwoPi = Math.PI * 2.0;

    /// <summary>
    /// Computes angular geometries for all enabled items.
    /// Returns an empty list if no items are enabled or total weight is zero.
    /// </summary>
    public static IReadOnlyList<WheelSlice> CalculateSlices(IEnumerable<WheelItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var enabledItems = items.Where(i => i.IsEnabled && i.Weight > 0).ToList();
        if (enabledItems.Count == 0)
        {
            return [];
        }

        double totalWeight = enabledItems.Sum(i => i.Weight);
        if (totalWeight <= 0)
        {
            return [];
        }

        var slices = new List<WheelSlice>(enabledItems.Count);
        double currentAngle = 0.0;

        for (int i = 0; i < enabledItems.Count; i++)
        {
            var item = enabledItems[i];
            double sweep = TwoPi * (item.Weight / totalWeight);
            double endAngle = currentAngle + sweep;
            double midAngle = currentAngle + (sweep / 2.0);
            double percentage = (item.Weight / totalWeight) * 100.0;

            slices.Add(new WheelSlice(
                Item: item,
                Index: i,
                StartAngle: currentAngle,
                EndAngle: endAngle,
                SweepAngle: sweep,
                MidAngle: midAngle,
                Percentage: percentage));

            currentAngle = endAngle;
        }

        return slices;
    }

    /// <summary>
    /// Evaluates current rotation angle at normalized time progress t in [0, 1].
    /// Ease-Out Cubic: θ(t) = θ_target - (θ_target - θ_0) * (1 - t)^3
    /// </summary>
    public static double EvaluateAngle(
        double startAngle,
        double targetAngle,
        double progress,
        DecelerationProfile profile = DecelerationProfile.EaseOutCubic)
    {
        double t = Math.Clamp(progress, 0.0, 1.0);

        if (profile == DecelerationProfile.Exponential)
        {
            const double k = 4.5;
            double denom = 1.0 - Math.Exp(-k);
            double factor = (1.0 - Math.Exp(-k * t)) / denom;
            return startAngle + (targetAngle - startAngle) * factor;
        }

        // Ease-Out Cubic (Standard formula from requirements)
        double factorCubic = 1.0 - t;
        return targetAngle - (targetAngle - startAngle) * factorCubic * factorCubic * factorCubic;
    }

    /// <summary>
    /// Determines which slice is currently under the pointer for the given wheel rotation angle.
    /// </summary>
    public static WheelSlice? GetWinningSlice(
        IReadOnlyList<WheelSlice> slices,
        double currentAngle,
        double pointerAngle = DefaultPointerAngle)
    {
        if (slices.Count == 0)
        {
            return null;
        }

        double normalizedPhi = NormalizeAngle(pointerAngle - currentAngle);

        for (int i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            if (normalizedPhi >= slice.StartAngle && normalizedPhi < slice.EndAngle)
            {
                return slice;
            }
        }

        // Handle edge rounding near 2*PI
        return slices[^1];
    }

    /// <summary>
    /// Calculates a random target angle that lands cleanly within the slice of a chosen winning item.
    /// </summary>
    public static SpinTarget CalculateTarget(
        IReadOnlyList<WheelSlice> slices,
        double currentAngle,
        int minRotations = 5,
        int maxRotations = 8,
        double durationSeconds = 4.5,
        Random? rng = null)
    {
        if (slices.Count == 0)
        {
            throw new InvalidOperationException("Cannot calculate spin target with no active slices.");
        }

        var random = rng ?? Random.Shared;

        // Weighted random selection
        double totalWeight = slices.Sum(s => s.Item.Weight);
        double pickValue = random.NextDouble() * totalWeight;
        double cumulative = 0.0;
        WheelSlice selectedSlice = slices[0];

        foreach (var slice in slices)
        {
            cumulative += slice.Item.Weight;
            if (pickValue <= cumulative)
            {
                selectedSlice = slice;
                break;
            }
        }

        // Pick a safe inner angle within the winning slice (15% to 85% to avoid divider ambiguity)
        double marginFraction = 0.15 + (random.NextDouble() * 0.70);
        double targetSliceAngle = selectedSlice.StartAngle + (selectedSlice.SweepAngle * marginFraction);

        // Calculate rotation required so that (DefaultPointerAngle - targetAngle) % 2PI == targetSliceAngle
        double targetRemainder = NormalizeAngle(DefaultPointerAngle - targetSliceAngle);
        double currentRemainder = NormalizeAngle(currentAngle);
        double extraRotation = NormalizeAngle(targetRemainder - currentRemainder);

        // Ensure adequate forward rotation
        if (extraRotation < 0.2)
        {
            extraRotation += TwoPi;
        }

        int rotations = random.Next(minRotations, maxRotations + 1);
        double totalTargetAngle = currentAngle + (rotations * TwoPi) + extraRotation;

        return new SpinTarget(
            WinningItem: selectedSlice.Item,
            WinningIndex: selectedSlice.Index,
            TargetAngle: totalTargetAngle,
            RotationsCount: rotations,
            DurationSeconds: durationSeconds);
    }

    /// <summary>
    /// Detects how many slice divider lines were crossed as the wheel rotated from previousAngle to currentAngle.
    /// Used for pointer tick trigger and audio playback cues.
    /// </summary>
    public static int DetectDividerCrossings(
        double previousAngle,
        double currentAngle,
        IReadOnlyList<WheelSlice> slices,
        double pointerAngle = DefaultPointerAngle)
    {
        if (slices.Count <= 1 || currentAngle <= previousAngle)
        {
            return 0;
        }

        double deltaAngle = currentAngle - previousAngle;
        int fullRotations = (int)(deltaAngle / TwoPi);
        int crossings = fullRotations * slices.Count;

        double deltaRemainder = deltaAngle % TwoPi;
        if (deltaRemainder <= 0.0001)
        {
            return crossings;
        }

        // Pointer local wheel coordinates: phi = pointerAngle - angle
        double phiStart = NormalizeAngle(pointerAngle - previousAngle);
        double phiEnd = NormalizeAngle(phiStart - deltaRemainder);

        foreach (var slice in slices)
        {
            double divider = slice.EndAngle;
            if (IsAngleBetweenDecreasing(divider, phiStart, phiEnd))
            {
                crossings++;
            }
        }

        return crossings;
    }

    /// <summary>
    /// Checks if targetAngle lies on the arc traversed from startAngle downwards (counter-clockwise) to endAngle.
    /// </summary>
    private static bool IsAngleBetweenDecreasing(double target, double start, double end)
    {
        if (start >= end)
        {
            // Simple interval [end, start]
            return target <= start && target > end;
        }

        // Interval wraps around 0 (e.g. start = 0.2, end = 6.0)
        return target <= start || target > end;
    }

    /// <summary>
    /// Normalizes any angle in radians to the range [0, 2*PI).
    /// </summary>
    public static double NormalizeAngle(double angle)
    {
        double normalized = angle % TwoPi;
        if (normalized < 0.0)
        {
            normalized += TwoPi;
        }
        return normalized;
    }
}

