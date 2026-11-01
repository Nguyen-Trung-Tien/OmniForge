---
name: reusable-code-and-ui-architecture
description: Master guide for code reusability (DRY), standardized Core helpers (ToolViewModelBase, ResultExtensions, ByteSizeFormatter, SecurityGuard), and shared Fluent XAML controls.
---

# Reusable Code, Standardized Patterns & UI Architecture Skill

Use this skill before writing any new class, ViewModel, or XAML page in **OmniForge**. Never reinvent utilities, dialogs, formatters, or XAML card layouts that already exist in the shared architecture.

---

## 1. Directory Structure & Assets

- Audit Script: [audit_architecture_reuse.ps1](scripts/audit_architecture_reuse.ps1)
- Reference Page: [StandardModularPageExample.xaml](examples/StandardModularPageExample.xaml)
- Reference ViewModel: [StandardModularViewModelExample.cs](examples/StandardModularViewModelExample.cs)
- Components Catalog: [reusable-components-index.json](resources/reusable-components-index.json)

---

## 2. Inventory of Reusable C# Components (Always Reuse First)

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

## 3. Inventory of Reusable XAML Controls & Styles (`OmniForge.App`)

Every XAML page in `OmniForge.App` must declare `xmlns:controls="using:OmniForge.App.Controls"` and compose its UI from the 4 standard building blocks:
- `<controls:ToolHeaderControl>`
- `<controls:SectionCardControl>`
- `<controls:StatusFooterControl>`
- `<controls:EmptyStateControl>`

See [StandardModularPageExample.xaml](examples/StandardModularPageExample.xaml) and [StandardModularViewModelExample.cs](examples/StandardModularViewModelExample.cs) for complete reference implementations.
