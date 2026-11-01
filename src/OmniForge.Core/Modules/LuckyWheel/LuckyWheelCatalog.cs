using OmniForge.Core.Common;

namespace OmniForge.Core.Modules.LuckyWheel;

/// <summary>
/// Predefined palettes, preset templates, and text import parsing utilities for Lucky Wheel.
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

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string text = line;
            double weight = 1.0;

            // Pattern: "Option Name: 2" or "Option Name, 2" or "Option Name\t2"
            int separatorIndex = line.LastIndexOfAny([':', ',', '\t', '|']);
            if (separatorIndex > 0 && separatorIndex < line.Length - 1)
            {
                string potentialWeightStr = line[(separatorIndex + 1)..].Trim();
                if (double.TryParse(potentialWeightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedWeight) && parsedWeight > 0)
                {
                    weight = parsedWeight;
                    text = line[..separatorIndex].Trim();
                }
            }
            else if (line.EndsWith(')'))
            {
                // Pattern: "Option Name (3)"
                int openParen = line.LastIndexOf('(');
                if (openParen > 0)
                {
                    string potentialWeightStr = line[(openParen + 1)..^1].Trim();
                    if (double.TryParse(potentialWeightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedWeight) && parsedWeight > 0)
                    {
                        weight = parsedWeight;
                        text = line[..openParen].Trim();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                string color = VibrantPalette[paletteIndex % VibrantPalette.Length];
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
}

