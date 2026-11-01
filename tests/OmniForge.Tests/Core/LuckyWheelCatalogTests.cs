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

    [Fact]
    public void ParseItemsFromText_WithHeaderRow_ShouldSkipHeaderCorrectly()
    {
        string raw = """
            Tên,Trọng số
            Nguyễn Văn A, 2.0
            Trần Thị B, 1.5
            Lê Văn C, 3.0
            """;

        var result = LuckyWheelCatalog.ParseItemsFromText(raw);

        Assert.True(result.IsSuccess);
        var items = result.Value;
        Assert.Equal(3, items.Count);
        Assert.Equal("Nguyễn Văn A", items[0].Text);
        Assert.Equal(2.0, items[0].Weight, precision: 2);
        Assert.Equal("Trần Thị B", items[1].Text);
        Assert.Equal(1.5, items[1].Weight, precision: 2);
        Assert.Equal("Lê Văn C", items[2].Text);
        Assert.Equal(3.0, items[2].Weight, precision: 2);
    }

    [Fact]
    public void ParseItemsFromText_WithCommaSeparatedSingleLine_ShouldParseAllTokens()
    {
        string raw = "An, Bình, Cường, Dũng, Hoa";

        var result = LuckyWheelCatalog.ParseItemsFromText(raw);

        Assert.True(result.IsSuccess);
        var items = result.Value;
        Assert.Equal(5, items.Count);
        Assert.Equal("An", items[0].Text);
        Assert.Equal("Bình", items[1].Text);
        Assert.Equal("Cường", items[2].Text);
        Assert.Equal("Dũng", items[3].Text);
        Assert.Equal("Hoa", items[4].Text);
    }

    [Fact]
    public async Task ParseFromExcelStreamAsync_WithValidOpenXmlZip_ShouldParseCorrectly()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            // 1. Add xl/sharedStrings.xml
            var ssEntry = archive.CreateEntry("xl/sharedStrings.xml");
            using (var writer = new StreamWriter(ssEntry.Open()))
            {
                writer.Write("""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="5" uniqueCount="5">
                        <si><t>Tên</t></si>
                        <si><t>Trọng số</t></si>
                        <si><t>Đội Vàng</t></si>
                        <si><t>Đội Đỏ</t></si>
                        <si><t>Đội Xanh</t></si>
                    </sst>
                    """);
            }

            // 2. Add xl/worksheets/sheet1.xml
            var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
            using (var writer = new StreamWriter(sheetEntry.Open()))
            {
                writer.Write("""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                        <sheetData>
                            <row r="1">
                                <c r="A1" t="s"><v>0</v></c>
                                <c r="B1" t="s"><v>1</v></c>
                            </row>
                            <row r="2">
                                <c r="A2" t="s"><v>2</v></c>
                                <c r="B2"><v>2.5</v></c>
                            </row>
                            <row r="3">
                                <c r="A3" t="s"><v>3</v></c>
                                <c r="B3"><v>1.5</v></c>
                            </row>
                            <row r="4">
                                <c r="A4" t="s"><v>4</v></c>
                                <c r="B4"><v>3.0</v></c>
                            </row>
                        </sheetData>
                    </worksheet>
                    """);
            }
        }

        memoryStream.Position = 0;
        var result = await LuckyWheelCatalog.ParseFromExcelStreamAsync(memoryStream);

        Assert.True(result.IsSuccess);
        var items = result.Value;
        Assert.Equal(3, items.Count);
        Assert.Equal("Đội Vàng", items[0].Text);
        Assert.Equal(2.5, items[0].Weight, precision: 2);
        Assert.Equal("Đội Đỏ", items[1].Text);
        Assert.Equal(1.5, items[1].Weight, precision: 2);
        Assert.Equal("Đội Xanh", items[2].Text);
        Assert.Equal(3.0, items[2].Weight, precision: 2);
    }

    [Fact]
    public void ExportItemsToCsv_ShouldProduceStandardCsv()
    {
        var items = new List<WheelItem>
        {
            new("Alice, Smith", 2.0, "#2563EB"),
            new("Bob", 1.0, "#E11D48")
        };

        string csv = LuckyWheelCatalog.ExportItemsToCsv(items);

        Assert.Contains("Name,Weight,Color,IsEnabled", csv, StringComparison.Ordinal);
        Assert.Contains("\"Alice, Smith\",2.00,#2563EB,True", csv, StringComparison.Ordinal);
        Assert.Contains("Bob,1.00,#E11D48,True", csv, StringComparison.Ordinal);
    }
}

