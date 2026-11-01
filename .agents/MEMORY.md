# OmniForge Institutional Memory & Learned Patterns (`MEMORY.md`)

This file acts as the persistent knowledge base for AI Agents and engineers working on **OmniForge Utility Suite**.
Whenever a bug is resolved, a WinUI 3 quirk is discovered, or a convention is refined, append it via `.\forge learn "<insight>"` or the `/learn` command.

---

## Core Architectural & WinUI 3 Guardrails

- **[2026-11-01]** (WinUI 3 Navigation): `Frame.Navigate(Type)` instantiates `Page` classes via their parameterless constructor (`new Page()`). Never put required constructor parameters on a XAML `Page`; resolve `ViewModel` via `public MyViewModel ViewModel { get; } = App.GetService<MyViewModel>();`.
- **[2026-11-01]** (Zero-Touch Plugins): `ModuleAutoDiscoveryExtensions.AddDiscoveredToolModules` automatically registers any non-abstract `IToolModule`, its `ViewModelType`, `ViewType`, and co-located `I[Module]Service` -> `[Module]Service` in the same namespace. Never manually edit `ServiceCollectionExtensions.cs` when adding a new tool module.
- **[2026-11-01]** (C# 13 MVVM Toolkit): Always use C# 13 `public partial` properties with `[ObservableProperty]` (`public partial string InputText { get; set; }`) instead of legacy `private string _inputText;` fields to avoid MVVMTK0045 warnings under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- **[2026-11-01]** (Atomic Settings Persistence): `LocalSettingsService` writes to `settings.json.tmp` and uses `File.Move(..., overwrite: true)` to guarantee atomic persistence and prevent corrupting user settings on sudden process termination.
- **[2026-11-01]** (Root CLI Workflow): Always execute builds, tests, scaffolding, and verification from the solution root using `.\forge <command>` (`verify`, `build`, `run`, `test`, `arch`, `new-module`, `new-test`, `learn`, `logs`, `doctor`) to avoid directory hopping and reduce agent token consumption.

- **[2026-11-01 11:49]** (Localization): Always use string.Create(CultureInfo.InvariantCulture, ...) when formatting numeric metrics (bytes, durations, percentages) so tests and UI remain deterministic on non-US locales like vi-VN.
