using OmniForge.Core.Modules.LuckyWheel;

namespace OmniForge.Tests.Core;

public sealed class LuckyWheelCatalogTests
{
    [Fact]
    public void ParseItemsFromText_WithDelimitersAndWeights_ShouldParseCorrectly()
    {
        string raw = """
            First Option: 2.5
            Second Option, 3.0
            Third Option (4.0)
            Fourth Option	1.5
            Simple Option
            """;

        var result = LuckyWheelCatalog.ParseItemsFromText(raw);

        Assert.True(result.IsSuccess);
        var items = result.Value;
        Assert.NotNull(items);
        Assert.Equal(5, items.Count);

        Assert.Equal("First Option", items[0].Text);
        Assert.Equal(2.5, items[0].Weight, precision: 2);

        Assert.Equal("Second Option", items[1].Text);
        Assert.Equal(3.0, items[1].Weight, precision: 2);

        Assert.Equal("Third Option", items[2].Text);
        Assert.Equal(4.0, items[2].Weight, precision: 2);

        Assert.Equal("Fourth Option", items[3].Text);
        Assert.Equal(1.5, items[3].Weight, precision: 2);

        Assert.Equal("Simple Option", items[4].Text);
        Assert.Equal(1.0, items[4].Weight, precision: 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n \r\n   ")]
    public void ParseItemsFromText_WithEmptyInput_ShouldReturnFailure(string emptyText)
    {
        var result = LuckyWheelCatalog.ParseItemsFromText(emptyText);
        Assert.False(result.IsSuccess);
        Assert.Equal("LuckyWheel.EmptyText", result.Error.Code);
    }

    [Fact]
    public void Presets_AllAvailablePresets_ShouldContainValidItems()
    {
        var presets = LuckyWheelCatalog.AvailablePresets;
        Assert.NotEmpty(presets);

        foreach (var presetName in presets)
        {
            var items = LuckyWheelCatalog.GetPreset(presetName);
            Assert.True(items.Count >= 2, $"Preset '{presetName}' must have at least 2 items");

            foreach (var item in items)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.Text));
                Assert.True(item.Weight > 0.0);
                Assert.StartsWith("#", item.Color);
                Assert.True(item.IsEnabled);
            }
        }
    }
}

