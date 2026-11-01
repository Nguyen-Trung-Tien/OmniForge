---
name: winui3-module-generator
description: Scaffolds a new OmniForge modular sub-tool complying with Clean Architecture, MVVM source generators, and IToolModule plugin registration.
---

# WinUI 3 Module Generator Skill

This skill guides agents and developers through creating a new modular utility sub-tool in the **OmniForge Utility Suite**.

## Overview of a Sub-Tool Module

Every sub-tool in OmniForge consists of four core components located in `src/OmniForge.App/Modules/[ToolName]/`:
1. **`[ToolName]Service.cs`**: Pure domain/business logic service (optional separate interface in `OmniForge.Core` if cross-cutting).
2. **`[ToolName]ViewModel.cs`**: MVVM ViewModel leveraging `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
3. **`[ToolName]Page.xaml` & `[ToolName]Page.xaml.cs`**: WinUI 3 Fluent Page using compiled `{x:Bind}`.
4. **`[ToolName]Module.cs`**: Descriptor implementing `IToolModule` for dynamic shell discovery.

---

## Step-by-Step Scaffolding Procedure

### Step 1: Create the Feature Directory
Create directory `src/OmniForge.App/Modules/[ToolName]/`.

### Step 2: Implement `[ToolName]Service.cs`
Handles the core business logic, decoupled from UI.

```csharp
namespace OmniForge.App.Modules.[ToolName];

public interface I[ToolName]Service
{
    Task<Result<string>> ExecuteOperationAsync(string input, CancellationToken cancellationToken = default);
}

public sealed class [ToolName]Service : I[ToolName]Service
{
    public async Task<Result<string>> ExecuteOperationAsync(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Result<string>.Failure(new Error("[ToolName].InvalidInput", "Input cannot be empty."));
            }

            // Simulate or execute non-blocking business logic
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            var output = $"Processed: {input}";

            return Result<string>.Success(output);
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Failure(new Error("[ToolName].Cancelled", "Operation was cancelled by the user."));
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(new Error("[ToolName].ExecutionError", ex.Message));
        }
    }
}
```

### Step 3: Implement `[ToolName]ViewModel.cs`
Derives from `ObservableObject` and uses MVVM source generators.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.[ToolName];

public partial class [ToolName]ViewModel(I[ToolName]Service service) : ObservableObject
{
    private readonly I[ToolName]Service _service = service ?? throw new ArgumentNullException(nameof(service));

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunOperationCommand))]
    private string _inputData = string.Empty;

    [ObservableProperty]
    private string _outputResult = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    private bool CanRunOperation => !string.IsNullOrWhiteSpace(InputData) && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRunOperation))]
    private async Task RunOperationAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _service.ExecuteOperationAsync(InputData, cancellationToken);
            if (result.IsSuccess)
            {
                OutputResult = result.Value!;
            }
            else
            {
                ErrorMessage = result.Error.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### Step 4: Implement `[ToolName]Page.xaml`
Fluent design with zero code-behind logic.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Page
    x:Class="OmniForge.App.Modules.[ToolName].[ToolName]Page"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:local="using:OmniForge.App.Modules.[ToolName]"
    mc:Ignorable="d">

    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="24">
        <StackPanel Spacing="16" MaxWidth="800" HorizontalAlignment="Left">
            <TextBlock Text="[Tool Display Name]" Style="{ThemeResource TitleTextBlockStyle}" />
            <TextBlock Text="Detailed description of what this tool performs."
                       Style="{ThemeResource BodyTextBlockStyle}"
                       Foreground="{ThemeResource TextFillColorSecondaryBrush}" />

            <InfoBar IsOpen="{x:Bind ViewModel.ErrorMessage, Mode=OneWay, Converter={StaticResource StringToBoolConverter}}"
                     Severity="Error"
                     Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />

            <TextBox Header="Input Data"
                     PlaceholderText="Enter data to process..."
                     Text="{x:Bind ViewModel.InputData, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="Execute"
                        Style="{ThemeResource AccentButtonStyle}"
                        Command="{x:Bind ViewModel.RunOperationCommand}" />
                <ProgressRing IsActive="{x:Bind ViewModel.IsBusy, Mode=OneWay}"
                              Width="24" Height="24" />
            </StackPanel>

            <TextBox Header="Result"
                     IsReadOnly="True"
                     TextWrapping="Wrap"
                     MinHeight="120"
                     Text="{x:Bind ViewModel.OutputResult, Mode=OneWay}" />
        </StackPanel>
    </ScrollViewer>
</Page>
```

And in `[ToolName]Page.xaml.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.[ToolName];

public sealed partial class [ToolName]Page : Page
{
    public [ToolName]ViewModel ViewModel { get; }

    public [ToolName]Page()
    {
        ViewModel = App.GetService<[ToolName]ViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
```

### Step 5: Implement `[ToolName]Module.cs`
Implements `IToolModule` contract.

```csharp
using OmniForge.Core.Contracts;

namespace OmniForge.App.Modules.[ToolName];

public sealed class [ToolName]Module : IToolModule
{
    public string Id => "omniforge.[toolname]";
    public string DisplayName => "[Tool Display Name]";
    public string Description => "Short summary of tool capabilities";
    public string IconGlyph => "\uE8B7"; // Use appropriate Segoe Fluent Icon glyph
    public Type ViewType => typeof([ToolName]Page);
    public Type ViewModelType => typeof([ToolName]ViewModel);
    public int Priority => 10;
    public bool IsEnabled => true;
}
```

### Step 6: Register in DI Container
In `src/OmniForge.App/Extensions/ServiceCollectionExtensions.cs`:
1. Register `services.AddSingleton<I[ToolName]Service, [ToolName]Service>();`
2. Register `services.AddTransient<[ToolName]ViewModel>();`
3. Register `services.AddTransient<[ToolName]Page>();`
4. Register the module in `IToolRegistryService`:
   `registry.RegisterModule(new [ToolName]Module());`

