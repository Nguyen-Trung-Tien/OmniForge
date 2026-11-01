# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Token-Efficient & Zero-Touch Plugin Architecture**:
  - `ToolViewModelBase` in `OmniForge.Core.Models` centralizing `IsBusy`, `IsNotBusy`, `StatusMessage`, `ErrorMessage`, `HasError`, and `RunSafeAsync(...)` execution helpers (reducing ViewModel code size by ~50%).
  - `ModuleAutoDiscoveryExtensions.AddDiscoveredToolModules` in `OmniForge.App.Extensions` automatically discovering and registering `IToolModule` plugins, Views, ViewModels, and co-located module Services without manual DI edits.
  - Reusable `ToolHeaderControl` (`OmniForge.App.Controls`) unifying module header typography and fault-isolated `InfoBar` error display.
  - 4 new AI Agent Skills: `token-efficient-coding`, `unit-test-scaffolder`, `crypto-hash-engine`, and `fluent-ui-design-system`.
- **Production Agentic Coding Infrastructure**:
  - Central Package Management (`Directory.Packages.props`) and root `.editorconfig` enforcing C# naming and style rules at compile time.
  - Automated Test & Architecture Verification suite (`tests/OmniForge.Tests`) using `xUnit` and `NetArchTest.Rules`.
  - Unified Agent verification pipeline (`.agents/scripts/verify-all.ps1` and `check-architecture.ps1`) plus `.agents/rules/testing-rules.md`.
  - GitHub Actions CI workflow (`.github/workflows/ci-verify.yml`).
  - Structured JSON-Lines background file logger (`FileLoggerProvider`) in `OmniForge.Services.Logging`.
  - UI Thread abstraction (`IDispatcherService` and `DispatcherService`) and global XAML value converters (`StringToBoolConverter`, `BoolToVisibilityConverter`).
  - Shell-level Module Fault Isolation error banner (`InfoBar`) in `MainWindow.xaml`.

### Changed
- Consolidated all domain service interfaces into `OmniForge.Core.Contracts` (removed redundant `OmniForge.Core.Interfaces` namespace).
- Upgraded `IFileSystemService` and `FileSystemService` to return functional `Result` and `Result<T>` envelopes and use `EnumerationOptions` (`IgnoreInaccessible = true`).

### Fixed
- Fixed runtime `NullReferenceException` on `DashboardPage` and `FileToolsPage` navigation by resolving ViewModels via `App.GetService<T>()` in page constructors.
- Fixed duplicate singleton registrations of `NavigationService` and `LocalSettingsService` across DI setup extensions.
- Updated `winui3-module-generator` skill and `scaffold_module.ps1` to generate WinUI `Frame.Navigate`-compatible parameterless page constructors.

---

## [0.1.0-alpha] - 2026-11-01

### Added
- **Clean Architecture Solution Scaffold**:
  - `OmniForge.Core`: Pure contracts layer containing `IToolModule`, `INavigationService`, `ILocalSettingsService`, and functional `Result<T>` pattern.
  - `OmniForge.Services`: Concrete implementations including `NavigationService`, `LocalSettingsService`, `FileSystemService`, and `ToolRegistryService`.
  - `OmniForge.App`: Windows App SDK / WinUI 3 presentation shell and composition root.
- **Global Build Configuration**:
  - `Directory.Build.props` enabling nullable reference types, implicit usings, C# latest language version, and treating warnings as errors.
- **Windows 11 Fluent Shell**:
  - `MainWindow` featuring `MicaAltBackdrop`, custom TitleBar integration, and `NavigationView` left-pane layout.
  - Dynamic module discovery and menu item binding in `MainWindowViewModel`.
  - Compile-time `{x:Bind}` support and zero logic in code-behind files.
- **Agent Governance & Rules Framework**:
  - Master architectural protocol in `.agents/AGENTS.md`.
  - Technical engineering rules: `winui3-rules.md`, `clean-code-rules.md`, and `async-io-rules.md`.
  - Antigravity AI Agent Skills: `winui3-module-generator`, `batch-io-processor`, and `win2d-canvas-renderer`.
- **System Documentation**:
  - `docs/ARCHITECTURE.md`: Complete blueprint of plugin system, navigation decoupling, and IoC container.
  - `docs/ROADMAP.md`: 5-Phase strategic plan from foundation to enterprise distribution.
  - `plans/phase-1-core-scaffold.md`: Granular checklist for Phase 1 execution.

