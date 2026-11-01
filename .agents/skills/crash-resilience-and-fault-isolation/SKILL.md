---
name: crash-resilience-and-fault-isolation
description: Defensive coding workflow to prevent WinUI 3 app crashes, unhandled COMExceptions, async void terminations, UI thread cross-access faults, and cancellation races.
---

# Crash Resilience & Fault Isolation Skill

Use this skill whenever writing ViewModels, asynchronous commands, XAML event handlers, or native WinRT interop in **OmniForge**. Under no circumstances may a module error crash `MainWindow` or terminate the process.

---

## 1. Directory Structure & Assets

- Audit Script: [check_crash_resilience.ps1](scripts/check_crash_resilience.ps1)
- Reference Implementation: [SafeViewModelCommandExample.cs](examples/SafeViewModelCommandExample.cs)
- Rules Matrix: [crash-resilience-rules.json](resources/crash-resilience-rules.json)

---

## 2. The 5 Non-Negotiable Anti-Crash Rules

1. **Zero `async void` Methods**:
   - `async void` exceptions bypass `try/catch` and crash the application dispatcher immediately.
   - Always use `[RelayCommand]` on `private async Task MyActionAsync(CancellationToken ct)` and wrap work inside `ToolViewModelBase.RunSafeAsync(...)`.
2. **Never Mutate UI Properties or Collections from `Task.Run`**:
   - Modifying an `[ObservableProperty]` or `ObservableCollection<T>` inside a `Task.Run(...)` delegate throws `COMException (0x8001010E: RPC_E_WRONG_THREAD)` in WinUI 3!
   - Compute results inside `Task.Run`, return `Result<T>` to the awaiting ViewModel method on the UI thread, and update properties **after** `await` completes (or via `IDispatcherService.TryEnqueue`).
3. **Graceful `OperationCanceledException` Handling**:
   - When a user cancels a long scan or switches tabs, `CancellationToken` throws `OperationCanceledException`.
   - `ToolViewModelBase.RunSafeAsync` catches `OperationCanceledException` automatically and sets `StatusMessage = "Operation was cancelled."` without flagging an error. In custom loops, always catch `OperationCanceledException` before `Exception`.
4. **Safe WinRT Picker / Clipboard / Window Interop**:
   - Never instantiate `FileOpenPicker`, `FolderPicker`, or `Clipboard` directly in ViewModels or Pages (which crashes in unpackaged WinUI 3 if `InitializeWithWindow` is forgotten).
   - Always inject `IFilePickerService` and `IClipboardService` which encapsulate HWND binding and wrap `COMException` into safe `Result<T>` failures.
5. **Safe XAML Value Converters & Null Guards**:
   - Never throw `NotImplementedException` in `IValueConverter.ConvertBack`—return `DependencyProperty.UnsetValue` so accidental TwoWay bindings never crash the XAML binding engine.

---

## 3. Standard Crash-Proof ViewModel Command Pattern

See [SafeViewModelCommandExample.cs](examples/SafeViewModelCommandExample.cs) for the complete reference implementation.
