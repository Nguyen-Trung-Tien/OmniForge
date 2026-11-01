using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using OmniForge.Core.Common;

namespace OmniForge.Core.Modules.LuckyWheel;

/// <summary>
/// Predefined palettes, preset templates, and smart file/text import parsing utilities for Lucky Wheel.
/// Centralized in Core for high performance and 100% testability.
/// </summary>
public static class LuckyWheelCatalog
{
    public static readonly string[] VibrantPalette =
    [
        "#E11D48", // Rose Red
        "#2563EB", // Royal Blue
        "#16A34A", // Emerald Green
        "#D97706", // Amber Gold
        "#9333EA", // Vivid Purple
        "#0D9488", // Deep Teal
        "#EA580C", // Bright Orange
        "#4F46E5", // Indigo
        "#0891B2", // Cyan
        "#DB2777", // Hot Pink
        "#CA8A04", // Mustard Yellow
        "#059669"  // Sea Green
    ];

    public static IReadOnlyList<string> AvailablePresets =>
    [
        "Decision Maker",
        "Lunch Chooser",
        "Prize Raffle",
        "Team Standup",
        "Dice Roll (1-6)"
    ];

    public static IReadOnlyList<WheelItem> GetPreset(string presetName)
    {
        return presetName switch
        {
            "Decision Maker" =>
            [
                new("Yes!", 2.0, VibrantPalette[2]),
                new("No", 2.0, VibrantPalette[0]),
                new("Maybe", 1.0, VibrantPalette[3]),
                new("Definitely", 1.5, VibrantPalette[1]),
                new("Ask Again", 1.0, VibrantPalette[4])
            ],
            "Lunch Chooser" =>
            [
                new("Pizza", 1.0, VibrantPalette[0]),
                new("Sushi", 1.0, VibrantPalette[1]),
                new("Burger", 1.0, VibrantPalette[2]),
                new("Salad", 1.0, VibrantPalette[3]),
                new("Tacos", 1.0, VibrantPalette[4]),
                new("Ramen / Pho", 1.0, VibrantPalette[5]),
                new("Korean BBQ", 1.0, VibrantPalette[6])
            ],
            "Prize Raffle" =>
            [
                new("🏆 Grand Prize", 0.5, VibrantPalette[3]),
                new("🥈 2nd Place", 1.0, VibrantPalette[1]),
                new("🥉 3rd Place", 1.5, VibrantPalette[4]),
                new("🎁 Gift Card", 2.0, VibrantPalette[0]),
                new("⭐ Bonus Spin", 2.5, VibrantPalette[2]),
                new("Better Luck Next Time", 4.0, VibrantPalette[5])
            ],
            "Team Standup" =>
            [
                new("Alex", 1.0, VibrantPalette[1]),
                new("Beatriz", 1.0, VibrantPalette[0]),
                new("Chen", 1.0, VibrantPalette[2]),
                new("Devon", 1.0, VibrantPalette[3]),
                new("Elena", 1.0, VibrantPalette[4]),
                new("Farhan", 1.0, VibrantPalette[5])
            ],
            "Dice Roll (1-6)" =>
            [
                new("1", 1.0, VibrantPalette[0]),
                new("2", 1.0, VibrantPalette[1]),
                new("3", 1.0, VibrantPalette[2]),
                new("4", 1.0, VibrantPalette[3]),
                new("5", 1.0, VibrantPalette[4]),
                new("6", 1.0, VibrantPalette[5])
            ],
            _ => GetPreset("Decision Maker")
        };
    }

    public static Result<IReadOnlyList<WheelItem>> ParseItemsFromText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.EmptyText", "Text cannot be empty.");
        }

        var lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.NoValidLines", "No valid lines found to import.");
        }

        var items = new List<WheelItem>(lines.Length);
        int paletteIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check if line 0 is a table header (e.g., "Name,Weight" or "Item\tWeight" or "Tên,Trọng số")
            if (i == 0 && IsLikelyHeaderRow(line))
            {
                continue;
            }

            // Case A: If user entered multiple items in one line separated by commas without numeric weights, e.g. "An, Bình, Cường, Dũng"
            if (lines.Length == 1 && line.Contains(',') && !line.Any(char.IsDigit))
            {
                var splitItems = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (splitItems.Length > 1)
                {
                    foreach (var token in splitItems)
                    {
                        if (string.IsNullOrWhiteSpace(token)) continue;
                        string col = VibrantPalette[paletteIndex % VibrantPalette.Length];
                        paletteIndex++;
                        items.Add(new WheelItem(token.Trim(), 1.0, col));
                    }
                    continue;
                }
            }

            string text = line;
            double weight = 1.0;
            string? customColor = null;

            // Check comma or tab separated values: Name, Weight, Color
            char[] separators = [',', '\t', ';', '|', ':'];
            int separatorIndex = line.LastIndexOfAny(separators);
            if (separatorIndex > 0 && separatorIndex < line.Length - 1)
            {
                var parts = line.Split(separators, StringSplitOptions.TrimEntries);
                if (parts.Length >= 2 && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double w) && w > 0)
                {
                    text = parts[0].Trim('"', '\'');
                    weight = w;
                    if (parts.Length >= 3 && parts[2].StartsWith('#') && (parts[2].Length == 7 || parts[2].Length == 9))
                    {
                        customColor = parts[2];
                    }
                }
                else
                {
                    string potentialWeightStr = line[(separatorIndex + 1)..].Trim();
                    if (double.TryParse(potentialWeightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedWeight) && parsedWeight > 0)
                    {
                        weight = parsedWeight;
                        text = line[..separatorIndex].Trim().Trim('"', '\'');
                    }
                }
            }
            else if (line.EndsWith(')'))
            {
                // Pattern: "Option Name (3)" or "Option Name (2.5)"
                int openParen = line.LastIndexOf('(');
                if (openParen > 0)
                {
                    string potentialWeightStr = line[(openParen + 1)..^1].Trim();
                    if (double.TryParse(potentialWeightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedWeight) && parsedWeight > 0)
                    {
                        weight = parsedWeight;
                        text = line[..openParen].Trim();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                string color = customColor ?? VibrantPalette[paletteIndex % VibrantPalette.Length];
                paletteIndex++;
                items.Add(new WheelItem(text, weight, color));
            }
        }

        if (items.Count == 0)
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.NoItems", "Could not parse any valid items from input.");
        }

        return Result.Success<IReadOnlyList<WheelItem>>(items);
    }

    /// <summary>
    /// Parses Excel (.xlsx) OpenXML files asynchronously using native BCL ZipArchive and XDocument.
    /// Zero third-party dependencies, high performance, trimming-safe.
    /// </summary>
    public static async Task<Result<IReadOnlyList<WheelItem>>> ParseFromExcelStreamAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            // 1. Load shared strings table if present
            var sharedStrings = new List<string>();
            var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
            if (sharedStringsEntry is not null)
            {
                await using var ssStream = sharedStringsEntry.Open();
                var ssDoc = await XDocument.LoadAsync(ssStream, LoadOptions.None, cancellationToken);
                XNamespace ssNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                foreach (var si in ssDoc.Descendants(ssNs + "si"))
                {
                    string val = string.Concat(si.Descendants(ssNs + "t").Select(t => t.Value));
                    sharedStrings.Add(val);
                }
            }

            // 2. Locate first worksheet
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

            if (sheetEntry is null)
            {
                return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.NoWorksheet", "No valid worksheet found in the Excel workbook.");
            }

            await using var sheetStream = sheetEntry.Open();
            var sheetDoc = await XDocument.LoadAsync(sheetStream, LoadOptions.None, cancellationToken);
            XNamespace sNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var rows = sheetDoc.Descendants(sNs + "row").ToList();
            if (rows.Count == 0)
            {
                return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.EmptyWorksheet", "The selected worksheet contains no rows.");
            }

            var items = new List<WheelItem>(rows.Count);
            int paletteIndex = 0;
            bool isFirstRow = true;

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cells = row.Elements(sNs + "c").ToList();
                if (cells.Count == 0) continue;

                // Extract cells by column index
                string? name = null;
                double weight = 1.0;
                string? color = null;

                foreach (var cell in cells)
                {
                    string cellRef = cell.Attribute("r")?.Value ?? string.Empty;
                    int colIdx = GetColumnIndex(cellRef);
                    string cellVal = GetCellValue(cell, sharedStrings, sNs);

                    if (colIdx == 0)
                    {
                        name = cellVal;
                    }
                    else if (colIdx == 1)
                    {
                        if (double.TryParse(cellVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedW) && parsedW > 0)
                        {
                            weight = parsedW;
                        }
                    }
                    else if (colIdx == 2 && cellVal.StartsWith('#'))
                    {
                        color = cellVal;
                    }
                }

                // If column A wasn't indexed by r="A1", fall back to position-based
                if (string.IsNullOrWhiteSpace(name) && cells.Count > 0)
                {
                    name = GetCellValue(cells[0], sharedStrings, sNs);
                    if (cells.Count > 1 && double.TryParse(GetCellValue(cells[1], sharedStrings, sNs), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedW) && parsedW > 0)
                    {
                        weight = parsedW;
                    }
                }

                if (string.IsNullOrWhiteSpace(name)) continue;

                // Skip header row if present on first evaluated row
                if (isFirstRow)
                {
                    isFirstRow = false;
                    if (IsLikelyHeaderRow(name))
                    {
                        continue;
                    }
                }

                string assignedColor = color ?? VibrantPalette[paletteIndex % VibrantPalette.Length];
                paletteIndex++;
                items.Add(new WheelItem(name.Trim(), weight, assignedColor));
            }

            if (items.Count == 0)
            {
                return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.NoValidRows", "No valid items could be parsed from the Excel file.");
            }

            return Result.Success<IReadOnlyList<WheelItem>>(items);
        }
        catch (InvalidDataException)
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.CorruptedExcel", "The file is not a valid Excel (.xlsx) file or is corrupted.");
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.ExcelReadError", $"Failed to read Excel workbook: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports items to standard CSV string format.
    /// </summary>
    public static string ExportItemsToCsv(IEnumerable<WheelItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var sb = new StringBuilder();
        sb.AppendLine("Name,Weight,Color,IsEnabled");
        foreach (var item in items)
        {
            string escapedName = item.Text.Contains(',') || item.Text.Contains('"') || item.Text.Contains('\n')
                ? $"\"{item.Text.Replace("\"", "\"\"")}\""
                : item.Text;
            sb.AppendLine(CultureInfo.InvariantCulture, $"{escapedName},{item.Weight.ToString("F2", CultureInfo.InvariantCulture)},{item.Color},{item.IsEnabled}");
        }
        return sb.ToString();
    }

    private static bool IsLikelyHeaderRow(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.Trim().ToLowerInvariant();

        return lower is "name" or "item" or "option" or "tên" or "đối tượng" or "mục"
            or "title" or "label" or "value" or "danh sách" or "stt" or "họ và tên"
            || lower.StartsWith("name,", StringComparison.Ordinal)
            || lower.StartsWith("item,", StringComparison.Ordinal)
            || lower.StartsWith("tên,", StringComparison.Ordinal)
            || lower.StartsWith("name\t", StringComparison.Ordinal)
            || lower.StartsWith("tên\t", StringComparison.Ordinal);
    }

    private static int GetColumnIndex(string cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        int col = 0;
        foreach (char ch in cellRef)
        {
            if (char.IsLetter(ch))
            {
                col = (col * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
            }
            else break;
        }
        return Math.Max(0, col - 1);
    }

    private static string GetCellValue(XElement cell, List<string> sharedStrings, XNamespace ns)
    {
        string? type = cell.Attribute("t")?.Value;
        if (type == "s")
        {
            string? val = cell.Element(ns + "v")?.Value;
            if (int.TryParse(val, out int idx) && idx >= 0 && idx < sharedStrings.Count)
            {
                return sharedStrings[idx];
            }
            return string.Empty;
        }

        if (type == "inlineStr")
        {
            var isElem = cell.Element(ns + "is");
            if (isElem is not null)
            {
                return string.Concat(isElem.Descendants(ns + "t").Select(t => t.Value));
            }
            return string.Empty;
        }

        return cell.Element(ns + "v")?.Value ?? string.Empty;
    }
}

