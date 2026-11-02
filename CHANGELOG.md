# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Modal Confirmation Dialog & Auto-Dismiss Notifications**:
  - Replaced inline warning infobars in `ToolHeaderControl` with an official modal popup `ContentDialog` featuring warning badges, message cards, and theme-synchronized styling.
  - Implemented 5-second auto-dismiss timers on Settings status notices, update notifications, and tool success messages.
- **UI/UX Architecture Standardization & Fluent Design System Overhaul**:
  - Standardized semantic tokens (`SpaceInsetMicro-Large`, `ControlCornerRadius`, `CardCornerRadius`) in `SharedStyles.xaml` for consistent visual hierarchy across all controls.
  - Integrated quick Waffle App Launcher (`\uED35`) in `MainWindow.xaml` pane header, allowing direct jumps to any utility module.
  - Upgraded Batch Rename with progressive disclosure overflow flyouts and a primary action `SplitButton` for instant execution and undo.
  - Added cryptographic digest export options to Checksum card and interactive category filters on Dashboard.
- **Desktop Window Startup Centering & Responsive Layout Overhaul**:
  - Implemented startup window centering in `MainWindow.xaml.cs` via `DisplayArea.GetFromWindowId`, accounting for multi-monitor setups, taskbars, and DPI scaling.
  - Tuned adaptive thresholds and narrow-state visual layouts (< 720px) across all modular utility pages.
- **Production-Grade Application Update Architecture & Settings Integration**:
  - Implemented decoupled contract `IUpdateService` supporting SemVer 2.0.0 checks, streaming downloads, SHA-256 verification, and safe installer launching.
  - Built rich update preferences and real-time update card in `SettingsPage.xaml` with download throughput indicators and release notes previews.
- **Batch Folder Creator UX & Architecture Overhaul**:
  - Smart Import modal dialog supporting Excel (`.xlsx`), CSV, TSV, and plain text.
  - Numbered Series generator, directory tree diagram generation, and ASCII preview switcher.

### Changed
- **Modernized `package.bat` Visual CLI & English Localization**:
  - Upgraded `package.bat` with native ANSI terminal color formatting, structured status badges, and English messaging.
  - Added real-time .NET SDK and Inno Setup 6 compiler detection, post-build binary verification, and installer size reporting.
- **Batch Folder & Lucky Wheel Layout Refactoring**:
  - Streamlined `BatchFolderPage.xaml` card hierarchy and grouped categorized actions.
  - Re-architected `LuckyWheelPage.xaml` layout to eliminate text clipping, layout shift, and viewport overflow.

### Fixed
- **WinUI 3 Unpackaged Publish XAML Resources & PRI Missing Bug**:
  - Fixed startup crash (`0xc000027b`, `XamlParseException: Cannot locate resource from 'ms-appx:///Views/MainWindow.xaml'`) in installed Inno Setup builds.
  - Added MSBuild target `CopyWinUIResourcesToPublishDir` in `OmniForge.App.csproj` to automatically bundle compiled `.xbf` binaries and `resources.pri` / `OmniForge.App.pri` into the publish directory.
  - Enhanced `forge.ps1` with a unified `package` command that builds, publishes, and compiles Inno Setup installers to `release/installer/`.
- **Dark/Light Mode Theme Synchronization & Mica Contrast**:
  - Fixed DWM Immersive Dark Mode synchronization (`DwmSetWindowAttribute`), preventing black-on-black sidebar text and caption button glitches.
  - Added fallback backgrounds for `RootGrid` when Mica is disabled or unsupported.
- **Confirmation Dialog Message Synchronization**:
  - Fixed race condition in `ToolHeaderControl.xaml.cs` where confirmation dialogs opened before message text was populated.
- **Crash Resilience & Forbidden `async void` Elimination**:
  - Replaced unprotected `async void` property-changed handlers in `SettingsViewModel` with fault-isolated `RunSafeAsync` wrappers.
  - Fixed UI-thread synchronous file I/O in `BatchRenameViewModel` by offloading scanning to `Task.Run`.
- **Case-Only Batch Rename Collision Detection**:
  - Resolved NTFS case-insensitive collision false-positives during case-only renames (`BatchRenameEngine.BuildPreview`).
- **Build Infrastructure & MSB3026 File Lock Elimination**:
  - Added `KillLockingProcessesBeforeBuild` to terminate lingering `Microsoft.UI.Xaml.Markup.Compiler.exe` and `OmniForge.App.exe` before compilation.

### Security
- **XML Entity Expansion (XXE / Billion Laughs) & Zip Bomb Defense**:
  - Added `SecurityGuard.CreateSafeXmlReaderSettings` enforcing `DtdProcessing = Prohibit`, `MaxCharactersFromEntities = 1024`, and document size caps.
  - Hardened `TextPayloadEngine`, `FolderParser`, and `LuckyWheelCatalog` against malicious payloads and zip bombs.
- **Multi-Segment Path & Reserved Device Name Hardening**:
  - Enhanced `SecurityGuard.ValidateSafePath` to inspect all path segments against reserved Windows device names (`CON`, `PRN`, `AUX`, `NUL`, etc.) and path traversal attempts.
