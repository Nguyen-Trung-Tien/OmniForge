# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Batch File & Path Renamer Tool Module**:
  - `IRenameRule` contract (`OmniForge.Core.Contracts`) and `FileMetadata` rich record model (`OmniForge.Core.Models`).
  - Concrete transformation rules (`OmniForge.Modules.BatchOps`):
    - `PrefixSuffixRule`: Prepend prefixes and append suffixes to filename stems.
    - `ReplaceRule`: Search & replace with plain text, Match Case, ReDoS-protected regular expressions (`matchTimeout: 250ms`), and token interpolation (`{index}`, `{date}`, `{parent}`, `{ext}`).
    - `ChangeCaseRule`: Transform letter casing to `lowercase`, `UPPERCASE`, or `Title Case`.
    - `NumberingRule`: Sequential numbering with custom start, step, padding formats (e.g. `001`), Suffix/Prefix positioning, and in-string `{num}` token replacement.
  - `BatchRenameEngine` & `IBatchRenameEngine` (`OmniForge.Modules.BatchOps`):
    - Live multi-rule collision detection: illegal filename characters, Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, etc.), intra-batch duplicate destination collisions, and existing disk target detection.
    - Transactional rollback snapshot (`RenameExecutionJournal`) supporting 100% reversible undo in reverse topological order.
    - Non-blocking asynchronous execution via worker thread offloading with `IProgress<BatchProgressReport>`, cancellation support, and streaming via `IAsyncEnumerable<RenameItemResult>`.
  - Presentation & Fluent UI (`OmniForge.App.Modules.BatchRename`):
    - `BatchRenameViewModel` deriving from `ToolViewModelBase` handling file picking (`IFilePickerService`), drag-and-drop ingestion, 200ms reactive debouncing, rule list pipeline management, and undo rollback.
    - `BatchRenamePage.xaml` featuring top `CommandBar` (Add Files/Folders, Clear, Run Rename, Undo), side rule builder cards with interactive editors, and virtualized `ListView` data grid with collision warning badges.
    - `BatchRenameModule` plugin auto-discovered by `ModuleAutoDiscoveryExtensions`.
  - Comprehensive unit test suite (`RenameRulesTests`, `BatchRenameEngineTests`) validating rule chaining, ReDoS safety, collision diagnostics, and disk transaction rollback.
- **Reusable Fluent UI Component Library & Design System**:
  - Centralized `Styles/SharedStyles.xaml` defining `PageScrollViewerStyle`, `PageRootStackPanelStyle`, `FluentCardBorderStyle`, `StatusBadgeBorderStyle`, and `MonospaceCodeTextBoxStyle`.
  - `SectionCardControl` (`OmniForge.App.Controls`) providing a reusable Fluent card container with header icon, title, subtitle, action slot, and `[ContentProperty(Name = nameof(InnerContent))]`.
  - `StatusFooterControl` (`OmniForge.App.Controls`) binding directly to `IsBusy`, `IsIndeterminate`, `ProgressPercent`, `StatusMessage`, and `LastDurationText` with a duration pill badge.
  - `EmptyStateControl` (`OmniForge.App.Controls`) providing a reusable empty-state placeholder for lists and data grids.
- **Standardized Shared Services & Functional Utilities**:
  - `IFilePickerService` & `FilePickerService` abstracting WinUI 3 `FileOpenPicker` and `FolderPicker` with automatic `MainWindow` HWND initialization.
  - `IClipboardService` & `ClipboardService` abstracting system clipboard read/write operations for 100% ViewModel testability.
  - `ByteSizeFormatter` (`OmniForge.Core.Common`) providing culture-invariant byte size (`1.5 MB`) and execution duration (`14 ms`) formatting.
  - `SecurityGuard` (`OmniForge.Core.Common`) enforcing Path Traversal defense, reserved Windows device name blocking, and payload DoS size limits.
  - `BulkObservableCollection<T>` (`OmniForge.Core.Common`) enabling batch UI updates (`ReplaceAll`, `AddRange`) in a single `Reset` notification to prevent UI freezes.
  - `ResultExtensions` (`OmniForge.Core.Common`) adding `Ensure`, `Tap`, `MapAsync`, and `BindAsync` functional combinators.
  - Upgraded `ToolViewModelBase` with built-in high-resolution `Stopwatch.GetTimestamp()` execution timing (`LastDurationText`) and `ReportProgress(percent, status)`.
- **Unified Root CLI Task Runner (`.\forge.ps1` & `.\forge.cmd`)**:
  - Single-entrypoint terminal CLI at workspace root supporting `build`, `run`, `test`, `verify`, `arch`, `new-module`, `new-test`, `learn`, `logs`, `doctor`, `clean`, and `publish` without navigating into subdirectories.
  - Persistent institutional memory store (`.agents/MEMORY.md`) integrated with `.\forge learn "<insight>"`.
  - Root `global.json` SDK configuration and `.vscode/tasks.json` IDE task bindings.
  - 8 new AI Agent Skills: `cli-task-runner`, `security-and-input-hardening`, `performance-and-memory-optimization`, `crash-resilience-and-fault-isolation`, `reusable-code-and-ui-architecture`, `text-payload-inspector`, `regex-bulk-renamer`, and `msix-release-packager` (bringing the total to 15 specialized skills).
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
- Hardened `LocalSettingsService` with atomic write-through (`settings.json.tmp` + `File.Move(..., overwrite: true)`) to prevent settings corruption on abrupt termination.
- Optimized `FileLoggerProvider` background queue draining to batch multiple queued log entries per file stream flush cycle.
- Standardized `ToolModuleBase` to C# 13 `public partial` observable properties and upgraded `DashboardViewModel` / `DashboardPage.xaml` to use `ToolViewModelBase` and `ToolHeaderControl`.

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

