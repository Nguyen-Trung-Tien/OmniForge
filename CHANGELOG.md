# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Fixed
- **Lucky Wheel Weight Stepper Direct Numeric Input & Fixed Range Clamping**:
  - Replaced read-only `TextBlock` in `LuckyWheelPage.xaml` weight stepper with interactive `TextBox` supporting direct numeric input, Enter key commit via `WeightTextBox_KeyDown`, and two-way binding.
  - Constrained weight values to a fixed scale of `[0.0, 10.0]` (clamping typed numbers, increment/decrement buttons, and quick-add default inputs).
  - Enhanced parsing in `WheelItemViewModel` to support comma or period decimals (`5,5` or `5.5`), negative numbers clamped to `0.0`, and percentage suffixes (`10%`).
  - Widened the table column width from 92px to 96px for comfortable numeric editing without truncation.
- **Batch Folder Preview Panel Layout**:
  - Restructured `BatchFolderPage.xaml` PreviewCard header layout. Separated title/view toggles, status metric badges (`Total`, `New`, `Exists`, `Errors`), and search filter into dedicated rows. Resolved severe vertical word-wrap and character squishing ("📁 Xe / Phân / cấp / thư / mục...") caused by competing `Auto` and `*` columns.
- **Dashboard Quick Launch Card Layout & Category Badge**:
  - Restructured `DashboardPage.xaml` utility cards by elevating the category badge to an eyebrow tag above the tool title. Increased `ItemsWrapGrid` `ItemWidth` from 360px to 370px and tightened layout spacing. Eliminated category tag truncation and clipping (`[D]`, `[Devel]`, `[File Syst]`, `[File S]`, `[Se]`) while granting 100% full width to Vietnamese and English module titles.

### Added
- **Desktop Window Startup Centering & Responsive Layout Overhaul**:
  - Implemented desktop window startup centering in `MainWindow.xaml.cs` using `Microsoft.UI.Windowing.DisplayArea.GetFromWindowId` and `DisplayAreaFallback.Primary`. Accurately aligns the window in the center of the display work area while accounting for multi-monitor arrangements, DPI scaling, and Windows taskbars.
  - Tuned `NavigationView` adaptive threshold widths (`ExpandedModeThresholdWidth="1008"`, `CompactModeThresholdWidth="680"`) in `MainWindow.xaml` for smooth transitions between expanded sidebar, compact rail, and overlay drawer modes.
  - Enhanced `ToolHeaderControl` and `SectionCardControl` with responsive `TextWrapping="Wrap"` on titles and subtitles, eliminating text clipping on small displays or high DPI scaling factors.
  - Added adaptive visual states (`NarrowState` < 720px) to `SettingsPage.xaml` so update preferences, download action buttons, and channel selectors cleanly stack into responsive vertical flows.
  - Responsive refactoring across modular utility pages (`FileToolsPage`, `TextInspectorPage`, `ChecksumPage`, `BatchRenamePage`), ensuring command toolbars, search/filter inputs, and execution actions wrap or stack gracefully without horizontal scroll or control truncation.
- **Production-Grade Application Update Architecture & Settings Integration**:
  - Implemented decoupled contract `IUpdateService` in `OmniForge.Core.Contracts` with support for SemVer version checks, streaming downloads, SHA-256 cryptographic verification, and safe process launching.
  - Built immutable SemVer 2.0.0 parser and comparer `SemanticVersion` handling pre-release tags, numeric/lexical sorting, and build metadata.
  - Implemented resilient `UpdateService` in `OmniForge.Services` featuring:
    - Zero-allocation streaming downloads using `ArrayPool<byte>.Shared` and on-the-fly `IncrementalHash` (SHA-256).
    - HTTPS enforcement, path traversal defense via `SecurityGuard.ValidateSafePath`, and isolated temp workspace storage (`%TEMP%\OmniForge\Updates`).
    - Robust GitHub release manifest parsing supporting both `Stable` and `Preview` release channels.
    - Safe installer handoff (`.msix`, `.msixbundle`, `.appinstaller`, `.exe`) with extension verification and non-blocking asynchronous execution.
    - User preferences persistence for release channels (`Stable`/`Preview`) and auto-check on startup.
  - Overhauled `SettingsViewModel` and `SettingsPage.xaml` with a rich WinUI 3 Fluent Design card (`&#xE895;`):
    - Real-time update checking with in-button `ProgressRing`, last-checked timestamp, and up-to-date confirmation indicator.
    - Dynamic update-available banner with release title, download size pill, and expandable release notes preview.
    - Non-blocking download progress with throttled UI dispatch, byte throughput/speed indicators, and instant cancellation.
    - Verified ready-to-install card with single-click "Install & Relaunch".
    - Resilient error handling via dismissible and retryable `InfoBar`.
  - Added full bilingual localization (English & Tiếng Việt) for all 27 new update keys.
  - Added comprehensive unit test suites (`SemanticVersionTests` and `UpdateServiceTests`), increasing verified test count to 139 passing tests.
- **Batch Folder Creator UX & Architecture Overhaul**:
  - Smart Import Modal dialog with support for Excel (`.xlsx`), CSV, TSV, and Text (`.txt`) files or direct raw paste.
  - Multi-item Bulk Quick-Add support for comma-, semicolon-, or newline-separated folder names.
  - Numbered Series Generator with real-time live sample preview.
  - Prefix & Suffix batch applicator preserving line indentation.
  - Real-time search and filter box inside the live TreeView preview.
  - Comprehensive bilingual localization (English & Tiếng Việt) for all Batch Folder elements.
  - Fluent Segoe icon integration on all action buttons, pills, badges, tabs, and menus.
  - Additional domain templates: Web Full-Stack, Media & Video Production, and Business Office.
  - View switcher for Directory Preview: Interactive Tree View (`TreeView`) and Formatted ASCII/Unicode Text Tree Diagram (`FormattedTreeDiagram`) with single-click "Copy Diagram".
  - Attached property `TreeViewExtensions.RootNodesSource` for pure MVVM WinUI 3 tree hierarchy binding with zero code-behind.
  - Intelligent tree line extraction in `FolderParser.ExtractTreeLine` supporting ASCII branch markers (`├──`, `└──`, `│`), Markdown bullets (`- `, `* `, `• `), and inline subpath slashes (`src/components`).
  - Directory tree diagram generator `FolderParser.GenerateTreeDiagram` producing standard directory tree text with branch glyphs and status badges.
  - 11 unit tests in `FolderCreatorTests` covering Excel parsing, CSV/delimiter handling, bulk add, ASCII tree parsing, Markdown list parsing, inline slashes, and diagram generation.

### Fixed
- **Dark/Light Mode Theme Switching & System Chrome Synchronization**:
  - Resolved title bar caption button visibility glitch where buttons had opaque, contrasting background boxes in Dark/Light mode by enforcing `ButtonBackgroundColor`, `ButtonInactiveBackgroundColor`, `BackgroundColor`, and `InactiveBackgroundColor` to `Transparent`, and configuring contrast-compliant hover/pressed states.
  - Implemented Win32 DWM Immersive Dark Mode synchronization via `DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ...)` so window chrome, system borders, and composition match the requested theme.
  - Re-synchronized `SystemBackdrop` (`MicaBackdrop`) on dynamic theme changes and OS theme switches via `ActualThemeChanged`, preventing mismatched dark-on-light or light-on-dark mica tint bleed-through.
  - Configured resilient root grid background fallback (`ApplicationPageBackgroundThemeBrush`) when Mica backdrop is disabled, eliminating solid black background rendering.
  - Replaced hardcoded `#CC000000` smoke screen overlays in `BatchFolderPage` and `LuckyWheelPage` with dynamic `{ThemeResource SmokeFillColorDefaultBrush}` for smooth Fluent light/dark modal presentation.

### Changed
- Re-architected `BatchFolderPage.xaml` layout with streamlined card hierarchy (Target Path, Settings Bar, Tools & Editor, Tree Preview, Action Bar).
- Grouped tool actions into clean categorized command menus (Import, Series, Templates, Tokens, Tools, Clear).
- Fixed WinUI 3 `TreeView` hierarchy binding issue where nested child folders were ignored in `ItemsSource`, switching to `RootNodesSource` with `node.Content = node;` so multi-level folder structures render accurately matching user text indentation.
- Removed noisy and cluttered `RelativePath` strings from preview tree rows, displaying clean folder names with full path in tooltips.
- Overhauled `LuckyWheelPage.xaml` visual display:
  - Eliminated Title/Subtitle text truncation and multi-line wrapping in `SectionCardControl` by relocating action buttons to a dedicated full-width command toolbar.
  - Replaced misplaced folder icon with native list icon (`\uE8FD`).
  - Removed cramped 92px stepper from quick-add bar to give full width to the item name text input.
  - Redesigned search bar and metrics badges into a spacious 2-tier layout preventing text clipping.
  - Fixed dark mode stepper background rendering in ListView by transitioning from `TextBox` to semibold `TextBlock`, eliminating unsightly black rectangles.
  - Optimized canvas height to `390px` to prevent vertical viewport overflow and clipping of spin controls.

