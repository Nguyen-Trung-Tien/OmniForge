---
name: token-efficient-coding
description: Enforces ultra-compact, high-precision C# 13 and XAML coding patterns to minimize token consumption while preserving 100% Clean Architecture compliance.
---

# Token-Efficient & High-Precision Coding Skill

Use this skill whenever writing, refactoring, or scaffolding C# and XAML code in **OmniForge**. Its goal is to reduce context and output token usage by **40–65%** while maintaining zero-warning compilation.

---

## 1. Context Discovery Rules (Save Input Tokens)

1. **Never Read Generated or Binary Directories**: Never inspect `bin/`, `obj/`, `.vs/`, or `Assets/*.png`.
2. **Use Scaffolding Scripts Over Manual Multi-File Writes**:
   - To create a new utility module, run `.agents/skills/winui3-module-generator/scripts/scaffold_module.ps1` instead of manually writing 4 boilerplate files.
   - To create a unit test, run `.agents/skills/unit-test-scaffolder/scripts/scaffold_test.ps1`.
3. **Zero-Touch Module Registration**:
   - Do **not** read or edit `ServiceCollectionExtensions.cs` or `App.xaml.cs` when adding a new `IToolModule`. `ModuleAutoDiscoveryExtensions.AddDiscoveredToolModules` registers `IToolModule`, `ViewModelType`, `ViewType`, and `I[Module]Service` automatically.

---

## 2. High-Density C# 13 Patterns (Save Output Tokens)

### 2.1 Primary Constructors + `ToolViewModelBase`
**Avoid verbose constructors and manual `IsBusy`/`ErrorMessage`/`try-catch` blocks.**
Instead of 55 lines of boilerplate, write 18 lines:

```csharp
namespace OmniForge.App.Modules.TextInspector;

public partial class TextInspectorViewModel(ITextInspectorService service) : ToolViewModelBase
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
            FormattedPayload = res;
    }
}
```

### 2.2 Compact `Result<T>` Service Pattern
Use expression-bodied switches and `Result.Success` / `Result.Failure<T>` helpers:

```csharp
namespace OmniForge.App.Modules.TextInspector;

public interface ITextInspectorService
{
    Task<Result<string>> FormatJsonAsync(string json, CancellationToken ct = default);
}

public sealed class TextInspectorService : ITextInspectorService
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
```

### 2.3 Compact XAML with `ToolHeaderControl`
Never repeat the 20-line Title + Subtitle + InfoBar block. Use `<controls:ToolHeaderControl>`:

```xml
<controls:ToolHeaderControl
    Title="JSON &amp; Payload Inspector"
    Subtitle="Validate, format, and minify structured payloads."
    HasError="{x:Bind ViewModel.HasError, Mode=OneWay}"
    ErrorMessage="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />
```

