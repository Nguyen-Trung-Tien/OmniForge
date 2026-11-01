using OmniForge.Core.Modules.LuckyWheel;

namespace OmniForge.Tests.Core;

public sealed class LuckyWheelEngineTests
{
    [Fact]
    public void EvaluateAngle_EaseOutCubic_ShouldMatchBoundaryAndInterpolation()
    {
        double start = 0.0;
        double target = 100.0;

        // t = 0 -> start
        double at0 = LuckyWheelEngine.EvaluateAngle(start, target, 0.0, DecelerationProfile.EaseOutCubic);
        Assert.Equal(start, at0, precision: 4);

        // t = 1 -> target
        double at1 = LuckyWheelEngine.EvaluateAngle(start, target, 1.0, DecelerationProfile.EaseOutCubic);
        Assert.Equal(target, at1, precision: 4);

        // t = 0.5: Ease-out cubic formula target - (target - start) * (1 - 0.5)^3 = 100 - 100 * 0.125 = 87.5
        double atHalf = LuckyWheelEngine.EvaluateAngle(start, target, 0.5, DecelerationProfile.EaseOutCubic);
        Assert.Equal(87.5, atHalf, precision: 4);
    }

    [Fact]
    public void CalculateSlices_ShouldPartitionTwoPiProportionally()
    {
        var items = new List<WheelItem>
        {
            new("Item 1", weight: 1.0),
            new("Item 2", weight: 3.0),
            new("Item 3", weight: 1.0, isEnabled: false), // Disabled, should be omitted
        };

        var slices = LuckyWheelEngine.CalculateSlices(items);

        Assert.Equal(2, slices.Count);
        Assert.Equal("Item 1", slices[0].Item.Text);
        Assert.Equal(25.0, slices[0].Percentage, precision: 2);
        Assert.Equal("Item 2", slices[1].Item.Text);
        Assert.Equal(75.0, slices[1].Percentage, precision: 2);

        // Slices start at 0 and end at 2*PI
        Assert.Equal(0.0, slices[0].StartAngle, precision: 5);
        Assert.Equal(LuckyWheelEngine.TwoPi, slices[1].EndAngle, precision: 5);
    }

    [Fact]
    public void CalculateTarget_ShouldLandInsideWinningItemSlice()
    {
        var items = new List<WheelItem>
        {
            new("Red", weight: 1.0, color: "#FF0000"),
            new("Green", weight: 2.0, color: "#00FF00"),
            new("Blue", weight: 1.0, color: "#0000FF"),
            new("Yellow", weight: 2.0, color: "#FFFF00"),
        };

        var slices = LuckyWheelEngine.CalculateSlices(items);
        double startAngle = 1.25;

        for (int i = 0; i < 50; i++)
        {
            var target = LuckyWheelEngine.CalculateTarget(slices, startAngle, minRotations: 4, maxRotations: 6);

            // Verify target angle rotates forward by at least minRotations
            Assert.True(target.TargetAngle >= startAngle + (4 * LuckyWheelEngine.TwoPi));

            // Verify that evaluating the winning slice at TargetAngle matches target.WinningItem
            var winningSlice = LuckyWheelEngine.GetWinningSlice(slices, target.TargetAngle);
            Assert.NotNull(winningSlice);
            Assert.Equal(target.WinningItem.Id, winningSlice.Item.Id);
        }
    }

    [Fact]
    public void DetectDividerCrossings_ShouldDetectCrossingsAccurately()
    {
        var items = new List<WheelItem>
        {
            new("A", 1.0),
            new("B", 1.0),
            new("C", 1.0),
            new("D", 1.0)
        };

        var slices = LuckyWheelEngine.CalculateSlices(items);

        // 1 full rotation (2*PI) must cross all 4 dividers
        int crossingsFull = LuckyWheelEngine.DetectDividerCrossings(0.0, LuckyWheelEngine.TwoPi, slices);
        Assert.Equal(4, crossingsFull);

        // 2 full rotations must cross 8 dividers
        int crossingsTwo = LuckyWheelEngine.DetectDividerCrossings(0.0, 2 * LuckyWheelEngine.TwoPi, slices);
        Assert.Equal(8, crossingsTwo);

        // Zero delta produces 0 crossings
        int crossingsZero = LuckyWheelEngine.DetectDividerCrossings(1.0, 1.0, slices);
        Assert.Equal(0, crossingsZero);
    }

    [Fact]
    public void CalculateSlices_WithNoEnabledItems_ReturnsEmpty()
    {
        var items = new List<WheelItem>
        {
            new("A", 1.0, isEnabled: false),
            new("B", 0.0, isEnabled: true) // zero weight
        };

        var slices = LuckyWheelEngine.CalculateSlices(items);
        Assert.Empty(slices);
    }
}

