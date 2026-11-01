---
name: winui3-module-generator
description: Scaffolds a new OmniForge modular sub-tool complying with Clean Architecture, ToolViewModelBase, ToolHeaderControl, and zero-touch IToolModule auto-discovery.
---

# WinUI 3 Module Generator Skill (Token-Efficient Edition)

This skill guides agents through creating a new modular utility sub-tool in **OmniForge** with minimal boilerplate and zero manual DI editing.

---

## 1. Directory Structure & Assets

- Scaffolding Script: [scaffold_module.ps1](scripts/scaffold_module.ps1)
- Reference Module: [SampleToolModule.cs](examples/SampleToolModule.cs)
- ViewModel Template: [ViewModelTemplate.cs](resources/ViewModelTemplate.cs)
- View XAML Template: [ViewTemplate.xaml](resources/ViewTemplate.xaml)

---

## 2. Architecture Advantages (Zero-Touch Auto-Discovery)

1. **Automatic DI Registration**: `ModuleAutoDiscoveryExtensions` scans the assembly on startup. Any class implementing `IToolModule` has its `ViewType`, `ViewModelType`, and co-located `I[ToolName]Service` -> `[ToolName]Service` automatically registered in DI and `IToolRegistryService`. **Do NOT edit `ServiceCollectionExtensions.cs` or `App.xaml.cs` when adding a module.**
2. **`ToolViewModelBase` Inheritance**: Inherit from `ToolViewModelBase` instead of `ObservableObject` to get `IsBusy`, `IsNotBusy`, `StatusMessage`, `ErrorMessage`, `HasError`, and `RunSafeAsync(...)` out of the box.
3. **`ToolHeaderControl` Reusable UI**: Use `<controls:ToolHeaderControl>` in XAML to render the title, subtitle, and fault-isolated `InfoBar` in 4 lines.
4. **One-Command Scaffolding**: Always run `.agents/skills/winui3-module-generator/scripts/scaffold_module.ps1` to generate all 4 files + unit test in 1 tool call instead of writing 5 separate files manually.

---

## Fast-Path Execution (Recommended for AI Agents)

Run the scaffolding script directly:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .agents/skills/winui3-module-generator/scripts/scaffold_module.ps1 `
  -ModuleName "HashValidator" `
  -DisplayName "Hash Validator" `
  -Category "Security & Cryptography" `
  -IconGlyph "\uE8D7" `
  -IncludeUnitTest
```

Then verify immediately:
```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/verify-all.ps1
```

---

## Compact Reference Templates

### 1. `[ToolName]Service.cs`
```csharp
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.[ToolName];

public interface I[ToolName]Service
{
    Task<Result<string>> ProcessAsync(string input, CancellationToken cancellationToken = default);
}

public sealed class [ToolName]Service : I[ToolName]Service
{
    public async Task<Result<string>> ProcessAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<string>("[ToolName].InvalidInput", "Input cannot be empty.");

        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        return Result.Success($"Processed: {input}");
    }
}
```

### 2. `[ToolName]ViewModel.cs` (Using `ToolViewModelBase` & Primary Constructor)
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Models;

namespace OmniForge.App.Modules.[ToolName];

public partial class [ToolName]ViewModel(I[ToolName]Service service) : ToolViewModelBase
{
    private readonly I[ToolName]Service _service = service ?? throw new ArgumentNullException(nameof(service));

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutputResult { get; set; } = string.Empty;

    private bool CanExecuteAction => !string.IsNullOrWhiteSpace(InputText) && IsNotBusy;

    [RelayCommand(CanExecute = nameof(CanExecuteAction))]
    private async Task ExecuteActionAsync(CancellationToken cancellationToken)
    {
        var output = await RunSafeAsync(
            ct => _service.ProcessAsync(InputText, ct),
            busyStatus: "Processing...",
            successStatus: "Completed.",
            cancellationToken: cancellationToken);

        if (output is not null)
        {
            OutputResult = output;
        }
    }
}
```

### 3. `[ToolName]Page.xaml` & `[ToolName]Page.xaml.cs`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Page
    x:Class="OmniForge.App.Modules.[ToolName].[ToolName]Page"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:OmniForge.App.Controls">

    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="32,24,32,24">
        <StackPanel Spacing="16" MaxWidth="800" HorizontalAlignment="Left">
            <controls:ToolHeaderControl
                Title="[DisplayName]"
                Subtitle="Modular utility tool for OmniForge Suite."
                HasError="{x:Bind ViewModel.HasError, Mode=OneWay}"
                ErrorMessage="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />

            <TextBox Header="Input Payload"
                     Text="{x:Bind ViewModel.InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

            <StackPanel Orientation="Horizontal" Spacing="12">
                <Button Content="Execute"
                        Style="{ThemeResource AccentButtonStyle}"
                        Command="{x:Bind ViewModel.ExecuteActionCommand}" />
                <ProgressRing IsActive="{x:Bind ViewModel.IsBusy, Mode=OneWay}" Width="20" Height="20" />
            </StackPanel>

            <TextBox Header="Output Result"
                     IsReadOnly="True"
                     TextWrapping="Wrap"
                     MinHeight="120"
                     Text="{x:Bind ViewModel.OutputResult, Mode=OneWay}" />
        </StackPanel>
    </ScrollViewer>
</Page>
```

```csharp
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.[ToolName];

public sealed partial class [ToolName]Page : Page
{
    public [ToolName]ViewModel ViewModel { get; } = App.GetService<[ToolName]ViewModel>();

    public [ToolName]Page()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }
}
```

### 4. `[ToolName]Module.cs`
```csharp
using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.[ToolName];

public sealed class [ToolName]Module : IToolModule
{
    public string Id => "omniforge.[toolname]";
    public string DisplayName => "[DisplayName]";
    public string Description => "Short summary of tool capabilities";
    public string IconGlyph => "\uE8B7";
    public string Category => "Developer Tools";
    public Type ViewType => typeof([ToolName]Page);
    public Type ViewModelType => typeof([ToolName]ViewModel);
    public int Priority => 50;
}
```
