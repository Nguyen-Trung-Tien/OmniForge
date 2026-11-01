# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Fixed
- **Thread Marshaling & Unobserved Task Exception (COMException 0x8001010E)**:
  - Fixed `RPC_E_WRONG_THREAD` fault in [FolderCreatorViewModel.cs](file:///d:/Tools/OmniForge/src/OmniForge.App/Modules/BatchFolder/FolderCreatorViewModel.cs) caused by instantiating `FolderPreviewNode` (`TreeViewNode` / WinUI `DependencyObject`) on a background worker thread (`Task.Run`). Moved tree node construction inside `_dispatcherService.TryEnqueue(...)` onto the UI thread.
  - Added global error guarding with `try/catch` in `TriggerDebouncedPreview()` and `TriggerDebouncePreview()` ([BatchRenameViewModel.cs](file:///d:/Tools/OmniForge/src/OmniForge.App/Modules/BatchRename/BatchRenameViewModel.cs)) to catch and marshal any operational errors, eliminating unobserved task exceptions rethrown by the finalizer thread.
  - Hardened fire-and-forget continuations in [MainWindow.xaml.cs](file:///d:/Tools/OmniForge/src/OmniForge.App/Views/MainWindow.xaml.cs) by explicitly observing `t.Exception` on faulted tasks.
- **UI Layout & Alignment Symmetry Across All Pages**:
  - Removed restrictive `MaxWidth="1040"` from `PageRootStackPanelStyle` in [SharedStyles.xaml](file:///d:/Tools/OmniForge/src/OmniForge.App/Styles/SharedStyles.xaml) and `MaxWidth="1280"` from [LuckyWheelPage.xaml](file:///d:/Tools/OmniForge/src/OmniForge.App/Modules/LuckyWheel/LuckyWheelPage.xaml). This eliminates the asymmetrical layout issue where pages were constrained to 1040px and left-aligned with a massive dead void on the right, allowing all pages to symmetrically stretch with uniform `OmniPagePadding` (`36,24,36,24`).
  - Added `HorizontalScrollBarVisibility="Disabled"` to `PageScrollViewerStyle` to prevent unbounded horizontal measurement and enforce responsive horizontal stretching across all display resolutions.
  - Removed hardcoded solid `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"` in [LuckyWheelPage.xaml](file:///d:/Tools/OmniForge/src/OmniForge.App/Modules/LuckyWheel/LuckyWheelPage.xaml) to preserve seamless MicaAlt backdrop consistency with the rest of the application.
- **Build Warnings & Process Lock Prevention**:
  - Added `<EnableDefaultXamlItems>false</EnableDefaultXamlItems>` to [OmniForge.Services.csproj](file:///d:/Tools/OmniForge/src/OmniForge.Services/OmniForge.Services.csproj) to eliminate unnecessary XAML compilation steps for the non-XAML service assembly.
  - Enhanced [forge.ps1](file:///d:/Tools/OmniForge/forge.ps1) `build` and `run` commands to terminate lingering `OmniForge.App` processes prior to execution, preventing `MSB3026` / `MSB3027` file lock warnings caused by active binaries during recompilation.
- **Navigation Shell & Feature Accessibility**:
  - Fixed issue where clicking navigation items in the left `NavigationView` pane failed to navigate to tool modules due to invalid `args.InvokedItemContainer?.DataContext` check. Added support for `args.InvokedItem`, `args.InvokedItemContainer?.Tag`, `DataContext`, `Content`, and fallback title matching.
  - Added two-way binding on `SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}"` and hooked up `SelectionChanged="NavView_SelectionChanged"` for keyboard navigation and selection synchronization.
  - Fixed Dashboard quick-launch "Launch" button failure caused by cross-template `Binding ElementName=RootPage` evaluation. Implemented strongly typed `DashboardToolItemViewModel` with direct compiled `{x:Bind LaunchCommand}`.
  - Enhanced AutoSuggestBox query submission to accept dropdown clicks via `SuggestionChosen` and search matching by `DisplayName`, `Title`, and `Id`.
- **UI Layout & Alignment Symmetry**:
  - Removed redundant external `<NavigationView.Header>` and set `AlwaysShowHeader="False"`, eliminating duplicate section titles and resolving horizontal alignment mismatch between the 12px header and 36px page cards (`OmniPagePadding`).
  - Re-aligned shell `InfoBar` margins (`36,12,36,0`) to symmetrically align with content cards and `OmniPagePadding`.
  - Optimized `NavigationView.PaneHeader` logo dimensions (`24x24`) and margins (`12,12,12,10`) to prevent text cropping and achieve centered icon alignment in compact pane mode (`CompactPaneLength="48"`).

### Changed
- **Central Package Management (Directory.Packages.props) Dependency Upgrades**:
  - Upgraded `Microsoft.Extensions.DependencyInjection`, `Logging`, `Logging.Abstractions`, `Logging.Debug` from `9.0.2` to `10.0.14`.
  - Upgraded `Microsoft.Graphics.Win2D` from `1.3.2` to `1.4.0`.
  - Upgraded `Microsoft.NET.Test.Sdk` from `17.12.0` to `18.11.0`.
  - Upgraded `xunit.runner.visualstudio` from `3.0.2` to `4.0.1`.
  - Upgraded `coverlet.collector` from `6.0.4` to `10.1.0`.
  - Verified 100% build compatibility and all 87 automated unit & architecture tests passing with 0 warnings.

### Added
- **Enterprise-Grade Bilingual Localization Architecture (Tiếng Việt & English)**:
  - Implemented decoupled [ILocalizationService](file:///d:/Tools/OmniForge/src/OmniForge.Core/Contracts/ILocalizationService.cs) and domain models ([AppLanguage.cs](file:///d:/Tools/OmniForge/src/OmniForge.Core/Models/AppLanguage.cs)) supporting `en-US` and `vi-VN` in `OmniForge.Core`.
  - Added thread-safe [LocalizationService](file:///d:/Tools/OmniForge/src/OmniForge.Services/Localization/LocalizationService.cs) with OS culture auto-detection, persistent settings storage, fallback chain (`vi-VN` -> `en-US` -> `[Key]`), and professional software terminology catalogs.
  - Implemented [LocalizationManager](file:///d:/Tools/OmniForge/src/OmniForge.App/Helpers/LocalizationManager.cs) dynamic XAML bridge with indexer binding and zero-restart hot-reloading across the UI.
  - Updated [SettingsViewModel.cs](file:///d:/Tools/OmniForge/src/OmniForge.App/ViewModels/SettingsViewModel.cs) and [SettingsPage.xaml](file:///d:/Tools/OmniForge/src/OmniForge.App/Views/SettingsPage.xaml) with dynamic language selector card and live localization.
  - Enhanced [MainWindowViewModel.cs](file:///d:/Tools/OmniForge/src/OmniForge.App/ViewModels/MainWindowViewModel.cs) and [NavigationItemViewModel.cs](file:///d:/Tools/OmniForge/src/OmniForge.App/ViewModels/NavigationItemViewModel.cs) to dynamically localize window titles, search placeholders, and navigation module titles upon language switch.
  - Added full test suite in [LocalizationServiceTests.cs](file:///d:/Tools/OmniForge/tests/OmniForge.Tests/Services/LocalizationServiceTests.cs) verifying bilingual catalog accuracy, fallback logic, argument formatting, and settings persistence.
- **Standardized Agent Skills Architecture**:
  - Restructured all 15 workspace skills under `.agents/skills/` to strictly comply with the standard 4-component specification: `SKILL.md`, `scripts/`, `examples/`, and `resources/`.
  - Added dedicated utility scripts, reference implementations, and template/schema catalogs across all skills (`cli-task-runner`, `crash-resilience-and-fault-isolation`, `crypto-hash-engine`, `fluent-ui-design-system`, `msix-release-packager`, `performance-and-memory-optimization`, `regex-bulk-renamer`, `reusable-code-and-ui-architecture`, `security-and-input-hardening`, `text-payload-inspector`, `token-efficient-coding`, `unit-test-scaffolder`).
- **File Hash & Integrity Validator Tool Module**:
  - `StreamHashEngine` (`OmniForge.Core.Modules.Checksum`): Zero-allocation streaming cryptographic checksum computation using `IncrementalHash` and `ArrayPool<byte>.Shared` supporting MD5, SHA-1, SHA-256, SHA-384, and SHA-512 with `CancellationToken` and progress reporting.
  - `IChecksumService` & `ChecksumService` (`OmniForge.App.Modules.Checksum`): Clean service abstraction auto-discovered by `ModuleAutoDiscoveryExtensions`.
  - `ChecksumViewModel` & `ChecksumPage.xaml` (`OmniForge.App.Modules.Checksum`): Fluent UI with drag-and-drop file ingestion, file size formatting, lower-case hex digest presentation, and real-time hash comparison verification.
  - Unit test suite (`StreamHashEngineTests`) validating known test vectors, multi-buffer streams (>64 KB), zero-byte files, and match/mismatch comparisons.
- **Text & Payload Inspector Tool Module**:
  - `TextPayloadEngine` (`OmniForge.Core.Modules.TextInspector`): High-speed streamed JSON formatting & minification via `Utf8JsonWriter` / `JsonDocument`, XML indentation formatting via `XDocument`, Base64 & URL codecs, JWT token header/payload decoding, and line-by-line diff computation.
  - `ITextInspectorService` & `TextInspectorService` (`OmniForge.App.Modules.TextInspector`): Presentation service abstraction auto-discovered by `ModuleAutoDiscoveryExtensions`.
  - `TextInspectorViewModel` & `TextInspectorPage.xaml` (`OmniForge.App.Modules.TextInspector`): Multi-mode workspace with segmented tool switching, input/output monospaced editors, clipboard copy/paste, and visual diff viewer.
  - Unit test suite (`TextPayloadEngineTests`) validating JSON formatting/minification, XML formatting, Base64 roundtrip, URL encoding, JWT parsing, and line diff detection.
- **Interactive Dashboard Available Utilities**:
  - `DashboardViewModel` & `DashboardPage.xaml`: Added quick-launch module cards displaying icon, title, category, description, and direct launch button for all discovered utility modules.
- **Production Settings & Preferences Architecture**:
  - `ISettingsService` contract (`OmniForge.Core.Contracts`) & `SettingsService` implementation (`OmniForge.Services`):
    - Persistent configuration stored in `%LocalAppData%/OmniForge/settings.json`.
    - Live dynamic theme switching (`Light`, `Dark`, `Default`) applied immediately without requiring application restart.
    - Automatic state restoration for last opened module and main window dimensions (`Window.Width`, `Window.Height`).
    - Custom user templates storage engine categorized by module (`GetUserTemplatesAsync`, `SaveUserTemplateAsync`, `DeleteUserTemplateAsync`).
  - Settings UI and Shell integration:
    - `SettingsPage.xaml` & `SettingsViewModel`: Added live theme selector bound to `SettingsService`.
    - `MainWindow.xaml.cs` & `MainWindowViewModel.cs`: Theme observer, geometry restorer/saver on close, and last opened module restoration.
  - Integration test suite (`SettingsService_ThemeWindowSizeAndTemplates_ShouldPersistAndNotify`) verifying end-to-end persistence and reactive notifications.
- **Global Exception & Crash Telemetry**:
  - `App.xaml.cs`:
    - Intercepts WinUI 3 XAML `UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException`.
    - Thread-safe non-fatal `ContentDialog` displaying structured diagnostic crash reports (timestamp, source, exception type, message, HResult, stack trace, inner exception).
    - One-click "Copy to Clipboard" button enabling instant diagnostic sharing.
    - Dialog re-entrancy shielding (`Interlocked.CompareExchange`) and fault isolation keeping the host process responsive.
- **Deployment Packaging & GitHub Actions CI Workflow**:
  - Dual packaging configuration in `OmniForge.App.csproj`:
    - Supports unpackaged self-contained builds (`<WindowsPackageType>None</WindowsPackageType>` and `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`).
    - Supports packaged MSIX builds (`-p:WindowsPackageType=MSIX`).
    - Trimming and AOT safety guards (`PublishReadyToRun=True`, suppression of IL2026/IL2072 in `ModuleAutoDiscoveryExtensions`).
  - `.github/workflows/build.yml`:
    - Automated GitHub Actions workflow on push and pull requests to `main`/`develop` and version tags (`v*`).
    - Executes .NET 9 restore, static architectural checks, and xUnit/NetArchTest test suite.
    - Validates packaged MSIX build and publishes unpackaged self-contained x64 executable.
    - Generates standalone `OmniForge-win-x64-standalone.zip` distribution archive and uploads it as a release artifact.
- **Lucky Wheel (Vòng quay may mắn) Tool Module**:
  - `LuckyWheelEngine` (`OmniForge.Core.Modules.LuckyWheel`):
    - Pure physics and geometry calculation engine with zero UI dependencies.
    - Smooth spin deceleration using Ease-Out Cubic ($\theta(t) = \theta_{target} - (\theta_{target} - \theta_0) \times (1 - t)^3$) and Exponential profiles.
    - Deterministic target calculation mapping weighted items to target angles with boundary safety margins.
    - Real-time slice divider crossing detection for audio tick cues and physical ticker recoil.
  - `LuckyWheelCatalog` & `WheelItem` (`OmniForge.Core.Modules.LuckyWheel`):
    - Domain models for items, slices, deceleration profiles, and spin targets.
    - Curated 12-color high-contrast vibrant palette.
    - Built-in presets: "Decision Maker", "Lunch Chooser", "Prize Raffle", "Team Standup", "Dice Roll (1-6)".
    - Multi-line text import parser supporting `"Option: 2"`, `"Option, 2"`, `"Option (3)"`, and tab-delimited formats.
  - Presentation & Win2D Rendering (`OmniForge.App.Modules.LuckyWheel`):
    - `LuckyWheelViewModel` deriving from `ToolViewModelBase` with reactive slice recalculation, item editing, presets, clipboard import/copy, and celebratory victory presentation.
    - `LuckyWheelPage.xaml` & `LuckyWheelPage.xaml.cs` powered by Win2D (`CanvasControl`):
      - High-performance, DPI-aware 60/120 FPS render loop synchronized to `CompositionTarget.Rendering`.
      - Segmented pie slices with metallic rim, drop shadow, and amber divider studs.
      - Radially oriented labels along slice bisectors with clean text clipping.
      - Interactive multi-layer center hub with click-to-spin trigger.
      - Top ticker needle with damped harmonic spring-recoil animation upon divider crossings.
      - Celebratory 70-particle floating confetti physics simulation on spin completion.
      - Responsive right configuration panel with live option editing, custom weights, and presets.
      - Celebratory winner overlay dialog with "Spin Again", "Remove Winner", and clipboard copy.
    - `LuckyWheelModule` plugin auto-discovered by `ModuleAutoDiscoveryExtensions`.
  - Comprehensive unit test suites (`LuckyWheelEngineTests`, `LuckyWheelCatalogTests`) validating physics formulas, slice partitioning, winner target landing, and import parsing.
- **Batch Folder Creator Tool Module**:
  - `FolderParser` (`OmniForge.Core.Modules.FolderCreator`):
    - Mode A (Flat/List): Supports line-by-line directory declarations with relative path / subpath splitting (`/` and `\`).
    - Mode B (Tree/Hierarchy): Stack-based visual indentation parser supporting 2 spaces, 4 spaces, and Tabs for defining parent-child nested directory trees.
    - Path sanitizer replacing illegal Windows characters (`\ / : * ? " < > |` and control characters 0-31) with safe replacements, trimming trailing spaces/dots, and neutralizing reserved Windows device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`).
  - `TokenEvaluator` (`OmniForge.Core.Modules.FolderCreator`):
    - Dynamic variable token interpolation for `{date:format}`, `{time:format}`, `{guid}`, and auto-incrementing `{seq:width,start}` with ReDoS timeout protection.
  - `FolderCreatorEngine` & `IFolderCreatorEngine` (`OmniForge.Core.Contracts` & `OmniForge.Core.Modules.FolderCreator`):
    - Non-destructive dry-run inspection returning real-time directory tree preview with collision diagnostics.
    - Three conflict resolution modes: `SkipExisting`, `Overwrite` (merge), and `AppendSuffix` (e.g. `Folder (1)`).
    - Asynchronous directory creation on thread pool with `CancellationToken` support and fault isolation (individual folder failures do not abort remaining creations).
  - Presentation & Fluent UI (`OmniForge.App.Modules.BatchFolder`):
    - `FolderCreatorViewModel` deriving from `ToolViewModelBase` with 180ms debounced reactive preview, directory picker (`IFilePickerService`), token insert chips, preset templates, line number generation, and progress reporting.
    - `BatchFolderPage.xaml` featuring target root folder picker, mode & conflict selectors, multi-line monospaced input with line numbers column, live `TreeView` directory preview with status pill badges, and progress bar.
    - `BatchFolderModule` plugin automatically discovered and registered via `ModuleAutoDiscoveryExtensions`.
  - Comprehensive unit test suite (`FolderCreatorTests`) validating flat and indented tree parsing, token expansion, sequence increments, path sanitization, conflict resolutions, and fault isolation.
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
- Fixed WinUI 3 cross-thread marshalling fault (`RPC_E_WRONG_THREAD`) in `BatchRenameViewModel.TriggerDebouncePreview()` by injecting `IDispatcherService` and dispatching `PreviewItems.ReplaceAll(...)` updates safely to the UI thread.
- Wrapped drag-and-drop `OnDrop` handler in `BatchRenamePage.xaml.cs` in defensive `try/catch` to prevent unhandled application crash on corrupted or restricted file drop operations.
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

