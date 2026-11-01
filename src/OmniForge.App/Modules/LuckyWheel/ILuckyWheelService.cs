using OmniForge.Core.Common;
using OmniForge.Core.Modules.LuckyWheel;

namespace OmniForge.App.Modules.LuckyWheel;

public interface ILuckyWheelService
{
    IReadOnlyList<string> PaletteColors { get; }

    IReadOnlyList<WheelItem> GetPreset(string presetName);

    IReadOnlyList<string> GetAvailablePresets();

    Result<IReadOnlyList<WheelItem>> ParseItemsFromText(string rawText);

    IReadOnlyList<WheelItem> ShuffleItems(IEnumerable<WheelItem> items);
}

public sealed class LuckyWheelService : ILuckyWheelService
{
    public static readonly string[] DefaultPalette = LuckyWheelCatalog.VibrantPalette;

    public IReadOnlyList<string> PaletteColors => LuckyWheelCatalog.VibrantPalette;

    public IReadOnlyList<string> GetAvailablePresets() => LuckyWheelCatalog.AvailablePresets;

    public IReadOnlyList<WheelItem> GetPreset(string presetName) => LuckyWheelCatalog.GetPreset(presetName);

    public Result<IReadOnlyList<WheelItem>> ParseItemsFromText(string rawText) => LuckyWheelCatalog.ParseItemsFromText(rawText);

    public IReadOnlyList<WheelItem> ShuffleItems(IEnumerable<WheelItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var list = items.Select(i => i.Clone()).ToList();
        var rng = Random.Shared;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
