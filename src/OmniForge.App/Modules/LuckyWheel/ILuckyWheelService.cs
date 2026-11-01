using OmniForge.Core.Common;
using OmniForge.Core.Modules.LuckyWheel;

namespace OmniForge.App.Modules.LuckyWheel;

public interface ILuckyWheelService
{
    IReadOnlyList<string> PaletteColors { get; }

    IReadOnlyList<WheelItem> GetPreset(string presetName);

    IReadOnlyList<string> GetAvailablePresets();

    Result<IReadOnlyList<WheelItem>> ParseItemsFromText(string rawText);

    Task<Result<IReadOnlyList<WheelItem>>> ParseItemsFromExcelStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<WheelItem>>> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    IReadOnlyList<WheelItem> ShuffleItems(IEnumerable<WheelItem> items);

    string ExportItemsToCsv(IEnumerable<WheelItem> items);
}

public sealed class LuckyWheelService : ILuckyWheelService
{
    public static readonly string[] DefaultPalette = LuckyWheelCatalog.VibrantPalette;

    public IReadOnlyList<string> PaletteColors => LuckyWheelCatalog.VibrantPalette;

    public IReadOnlyList<string> GetAvailablePresets() => LuckyWheelCatalog.AvailablePresets;

    public IReadOnlyList<WheelItem> GetPreset(string presetName) => LuckyWheelCatalog.GetPreset(presetName);

    public Result<IReadOnlyList<WheelItem>> ParseItemsFromText(string rawText) => LuckyWheelCatalog.ParseItemsFromText(rawText);

    public Task<Result<IReadOnlyList<WheelItem>>> ParseItemsFromExcelStreamAsync(Stream stream, CancellationToken cancellationToken = default) =>
        LuckyWheelCatalog.ParseFromExcelStreamAsync(stream, cancellationToken);

    public async Task<Result<IReadOnlyList<WheelItem>>> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.InvalidPath", "Đường dẫn file không được để trống.");
        }

        if (!File.Exists(filePath))
        {
            return Result.Failure<IReadOnlyList<WheelItem>>("LuckyWheel.FileNotFound", $"Không tìm thấy file: {filePath}");
        }

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".xlsx")
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return await LuckyWheelCatalog.ParseFromExcelStreamAsync(stream, cancellationToken);
        }

        // Plain text formats: .csv, .tsv, .txt
        string text = await File.ReadAllTextAsync(filePath, cancellationToken);
        return LuckyWheelCatalog.ParseItemsFromText(text);
    }

    public string ExportItemsToCsv(IEnumerable<WheelItem> items) =>
        LuckyWheelCatalog.ExportItemsToCsv(items);

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
