using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.Example;

public interface ICompactInspectorService
{
    Task<Result<string>> FormatJsonAsync(string json, CancellationToken ct = default);
}

public sealed class CompactInspectorService : ICompactInspectorService
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public Task<Result<string>> FormatJsonAsync(string json, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(json))
                return Result.Failure<string>("Json.Empty", "Payload is empty.");
            try
            {
                using var doc = JsonDocument.Parse(json);
                return Result.Success(JsonSerializer.Serialize(doc.RootElement, Indented));
            }
            catch (JsonException ex)
            {
                return Result.Failure<string>("Json.Invalid", ex.Message);
            }
        }, ct);
}

public partial class CompactInspectorViewModel(ICompactInspectorService service) : ToolViewModelBase
{
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(FormatCommand))]
    public partial string RawPayload { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FormattedPayload { get; set; } = string.Empty;

    private bool CanFormat => !string.IsNullOrWhiteSpace(RawPayload) && IsNotBusy;

    [RelayCommand(CanExecute = nameof(CanFormat))]
    private async Task FormatAsync(CancellationToken ct)
    {
        if (await RunSafeAsync(t => service.FormatJsonAsync(RawPayload, t), "Formatting...", "Formatted.", ct) is { } res)
        {
            FormattedPayload = res;
        }
    }
}

