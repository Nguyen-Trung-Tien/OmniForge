---
name: reusable-code-and-ui-architecture
description: Master guide for code reusability (DRY), standardized Core helpers (ToolViewModelBase, ResultExtensions, ByteSizeFormatter, SecurityGuard), and shared Fluent XAML controls.
---

# Reusable Code, Standardized Patterns & UI Architecture Skill

Use this skill before writing any new class, ViewModel, or XAML page in **OmniForge**. Never reinvent utilities, dialogs, formatters, or XAML card layouts that already exist in the shared architecture.

---

## 1. Inventory of Reusable C# Components (Always Reuse First)

| Need | Standardized OmniForge Component | Namespace |
| :--- | :--- | :--- |
| **ViewModel Base (`IsBusy`, `ErrorMessage`, `RunSafeAsync`, Timing)** | `ToolViewModelBase` | `OmniForge.Core.Models` |
| **Module Descriptor Base** | `ToolModuleBase` / `IToolModule` | `OmniForge.Core.Models` |
| **Batch UI Collection (No UI Freeze)** | `BulkObservableCollection<T>` (`.ReplaceAll()`, `.AddRange()`) | `OmniForge.Core.Common` |
| **Functional Error Handling & Piping** | `Result`, `Result<T>`, `ResultExtensions` (`.Ensure()`, `.Tap()`, `.MapAsync()`, `.BindAsync()`) | `OmniForge.Core.Common` |
| **Path Traversal & Payload Size Security** | `SecurityGuard.ValidateSafePath()`, `SecurityGuard.ValidatePayloadSize()` | `OmniForge.Core.Common` |
| **Format File Sizes & Durations (`1.5 MB`, `14 ms`)** | `ByteSizeFormatter.FormatBytes()`, `ByteSizeFormatter.FormatDuration()` | `OmniForge.Core.Common` |
| **Non-Blocking Disk I/O** | `IFileSystemService` | `OmniForge.Core.Contracts` |
| **Native File / Folder Picker Dialogs** | `IFilePickerService` (`PickSingleFileAsync`, `PickFolderAsync`) | `OmniForge.Core.Contracts` |
| **System Clipboard Copy / Paste** | `IClipboardService` (`CopyText`, `GetTextAsync`) | `OmniForge.Core.Contracts` |
| **Persistent JSON Settings** | `ILocalSettingsService` | `OmniForge.Core.Contracts` |
| **UI Thread Marshaling** | `IDispatcherService` | `OmniForge.Core.Contracts` |

---

## 2. Inventory of Reusable XAML Controls & Styles (`OmniForge.App`)

Every XAML page in `OmniForge.App` must declare `xmlns:controls="using:OmniForge.App.Controls"` and compose its UI from these 4 building blocks and `Styles/SharedStyles.xaml`:

```xml
<Page
    x:Class="OmniForge.App.Modules.Sample.SamplePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:OmniForge.App.Controls">

    <!-- 1. Standard Page ScrollViewer & Root StackPanel -->
    <ScrollViewer Style="{StaticResource PageScrollViewerStyle}">
        <StackPanel Style="{StaticResource PageRootStackPanelStyle}">

            <!-- 2. Reusable Tool Header + Error InfoBar -->
            <controls:ToolHeaderControl
                Title="Tool Display Name"
                Subtitle="Concise explanation of the utility."
                HasError="{x:Bind ViewModel.HasError, Mode=OneWay}"
                ErrorMessage="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />

            <!-- 3. Reusable Fluent Section Card -->
            <controls:SectionCardControl
                Title="Configuration"
                Subtitle="Input parameters"
                IconGlyph="&#xE943;">
                <StackPanel Spacing="12">
                    <TextBox Style="{StaticResource MonospaceCodeTextBoxStyle}"
                             Text="{x:Bind ViewModel.InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
                </StackPanel>
            </controls:SectionCardControl>

            <!-- 4. Reusable Progress Bar + Status Message + Execution Duration Pill -->
            <controls:StatusFooterControl
                IsBusy="{x:Bind ViewModel.IsBusy, Mode=OneWay}"
                IsIndeterminate="{x:Bind ViewModel.IsIndeterminate, Mode=OneWay}"
                ProgressPercent="{x:Bind ViewModel.ProgressPercent, Mode=OneWay}"
                StatusMessage="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
                DurationText="{x:Bind ViewModel.LastDurationText, Mode=OneWay}" />

            <!-- 5. Reusable Empty State inside Results Card -->
            <controls:SectionCardControl Title="Results" IconGlyph="&#xE8A5;">
                <Grid MinHeight="220">
                    <controls:EmptyStateControl
                        Title="No results yet"
                        Subtitle="Run the tool above to inspect output."
                        IsEmpty="{x:Bind ViewModel.IsEmpty, Mode=OneWay}" />
                </Grid>
            </controls:SectionCardControl>

        </StackPanel>
    </ScrollViewer>
</Page>
```

