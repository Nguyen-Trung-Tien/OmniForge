---
name: winui3-startup-navigation-lifecycle
description: Resolves and prevents WinUI 3 initial module activation race conditions where navigation is invoked before Frame attachment. Use when diagnosing startup InfoBar errors, NavigationService uninitialized exceptions, or when wiring initial page transitions in MainWindow.
---

# WinUI 3 Startup Navigation Lifecycle Guardrail

## Problem Symptom
- Red InfoBar banner on startup: `Module '...' failed to activate: NavigationService has not been initialized with a Frame.`
- Visual fault isolation banner appears on app launch before user interaction.

## Root Cause
- In DI composition roots, `MainWindow` depends on `MainWindowViewModel`. DI resolves `MainWindowViewModel` first.
- Invoking navigation in ViewModel constructors or search filters triggers `NavigateTo` while `NavigationService._frame` is still `null`.
- WinUI 3 `Frame` can only safely be initialized after `InitializeComponent()` inside `MainWindow`.

## Resolution Procedure
1. Remove all automatic navigation triggers from ViewModel constructors and filter methods.
2. Expose an explicit `public async Task RestoreInitialSelectionAsync()` on the ViewModel.
3. In `MainWindow.xaml.cs`, call `ViewModel.RestoreInitialSelectionAsync()` only after `navService.Initialize(ContentFrame)` has completed.
4. Dispatch UI property updates (`SelectedItem`, `NavigateTo`) via `IDispatcherService.TryEnqueue`.

## Prevention Check
- Run `.\forge verify` to guarantee zero startup race conditions and 100% test pass.
