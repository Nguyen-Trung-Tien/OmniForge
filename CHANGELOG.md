# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Modal Popup Confirmation Dialog & 5-Second Auto-Dismiss Notifications**:
  - Replaced inline confirmation warning InfoBar in `ToolHeaderControl` with an official modal popup `ContentDialog` featuring a warning badge, title, styled message card, and theme-synchronized styling (`RequestedTheme = ActualTheme`) optimized for both Dark Mode and Light Mode.
  - Implemented 5-second auto-dismiss timers on Settings `StatusNotice` and `IsUpToDate` notifications in `SettingsViewModel` with cancellation token protection.
  - Added UI-thread 5-second auto-dismiss timer for `SuccessMessage` in `ToolHeaderControl`, ensuring all tool operation completion banners automatically disappear cleanly.
  - Replaced raw border and textblock indicators in `SettingsPage.xaml` with polished Fluent `InfoBar` components for `IsUpToDate` and `StatusNotice`.

### Security
- **XML Entity Expansion (XXE / Billion Laughs) & Zip Bomb Defense**:
  - Added `SecurityGuard.CreateSafeXmlReaderSettings` and `SecurityGuard.LoadSafeXmlAsync` enforcing `DtdProcessing = DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 1024`, and `MaxCharactersInDocument = 10 MB`.
  - Hardened `TextPayloadEngine.FormatXmlAsync`, `FolderParser.ParseFromExcelStreamAsync`, and `LuckyWheelCatalog.ParseFromExcelStreamAsync` against DTD/XXE attacks and oversized `.xlsx` zip entries.
  - Enforced `SecurityGuard.ValidatePayloadSize` across all `TextPayloadEngine` operations (`FormatJsonAsync`, `FormatXmlAsync`, `EncodeBase64`, `DecodeBase64`, `EncodeUrl`, `DecodeUrl`, `InspectJwt`), `FolderParser.ImportFromFileAsync`, and `UpdateService.ParseReleasesPayload` with `MaxDepth = 64` JSON limits.
- **Multi-Segment Path & Reserved Device Name Hardening**:
  - Enhanced `SecurityGuard.ValidateSafePath` to inspect every directory segment in a path (not only the leaf filename) against reserved Windows device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`) and trailing dots/spaces.
  - Added explicit `SecurityGuard.ValidateSafePath` verification on download destination paths and configured a 30-second HTTP timeout in `UpdateService`.

### Fixed
- **Confirmation Dialog Message Blank Box Bug**:
  - Fixed property synchronization order in `ToolHeaderControl.xaml.cs` where `IsConfirmationOpen = true` was assigned before `ConfirmationTitle` and `ConfirmationMessage`, triggering `ShowConfirmationDialogAsync` before the message text was populated and rendering an empty box.
  - Added direct fallback to `_subscribedVm.ConfirmationMessage` and `_subscribedVm.ConfirmationTitle`, and guarded `messageBorder` rendering so empty placeholder boxes are never displayed.
- **Settings Update Notification Polish & Friendly 404 Handling**:
  - Replaced raw unformatted HTTP 404 error strings with localized, friendly update notices (`Settings_Updates_ErrorNotFound` and `Settings_Updates_ErrorTitle`).
  - Added 5-second auto-dismiss timer via `SetUpdateErrorWithAutoDismiss` in `SettingsViewModel` with cancellation token support, and enabled manual dismissal via InfoBar 'X' button.
- **NavigationView Sidebar Pitch-Black Mica Bleed-Through in Light Theme**:
  - Added explicit ThemeDictionaries (`NavigationViewDefaultPaneBackground` and `NavigationViewExpandedPaneBackground`) to `NavView.Resources` mapping to `#F3F3F3` in Light Mode and `#202020` in Dark Mode.
  - Implemented high-contrast protective tint on `RootGrid.Background` in `MainWindow.xaml.cs` for Light Mode, ensuring the NavigationView pane and TitleBar never degrade to pitch black on systems with dark OS wallpapers or disabled DWM transparency.
- **Shell Theme Synchronization & Mica Contrast Bug**:
  - Fixed theme calculation in `MainWindow.xaml.cs` (`DetermineIsDark`) where `ActualTheme` evaluated synchronously to Dark during Light theme selection, causing `DwmSetWindowAttribute` to force DWM Immersive Dark Mode on a Light XAML tree (resulting in black-on-black invisible sidebar text and icons).
  - Explicitly synchronized `RequestedTheme` across `NavView`, `AppTitleBar`, and `ContentFrame`, passing determined theme state to DWM, caption buttons, and `UpdateBackdropAndBackground`.
  - Added fallback to `ApplicationPageBackgroundThemeBrush` for `RootGrid` when Mica is disabled or unsupported, preventing solid black HWND background bleed-through.
  - Removed duplicate icon in `AppTitleBar` that stacked awkwardly above the branded SVG logo in `NavigationView.PaneHeader`, setting a clean `Height="32"` title bar.
  - Added `AppLauncherFlyout?.Hide()` on item click to cleanly dismiss the App Launcher Waffle flyout upon navigation.
- **Case-Only Batch Rename Collision & Change Detection Bug**:
  - Fixed `BatchRenameEngine.BuildPreview` where `StringComparison.OrdinalIgnoreCase` prevented case-only renames (e.g., `ChangeCaseRule` converting `photo.jpg` to `PHOTO.jpg`) from being recognized as pending changes (`HasChange = true`), and prevented false-positive disk collision errors on case-insensitive NTFS volumes.
- **Crash Resilience & Forbidden `async void` Elimination**:
  - Replaced 5 unprotected `async partial void` property-changed callbacks in `SettingsViewModel` (`OnSelectedThemeChanged`, `OnEnableMicaAltChanged`, `OnSelectedLanguageChanged`, `OnSelectedChannelChanged`, `OnAutoCheckOnStartupChanged`) with fault-isolated `RunSettingUpdateSafeAsync` wrappers.
  - Protected `ToolHeaderControl.ConfirmActionButton_Click` with `try/catch` fault isolation.
  - Updated `StringToBoolConverter.ConvertBack` to return `DependencyProperty.UnsetValue` instead of throwing `NotSupportedException`, and added `"Invert"` parameter support to `BoolToVisibilityConverter.ConvertBack`.
  - Fixed thread-safety race conditions and UI-thread synchronous I/O in `BatchRenameViewModel.TriggerDebouncePreview` and `ImportPathsAsync` by capturing immutable snapshots on the UI thread and offloading file discovery/metadata reading to `Task.Run`.
- **UI/UX, Memory & Localization Polish**:
  - Cached `SolidColorBrush` instances in `WheelItemViewModel` (`Brush` and `ContrastForegroundBrush`), eliminating redundant WinRT COM brush allocations on every XAML binding read.
  - Added missing `LocalizationKeys.Common_Open` (`"Open"` / `"Mở"`) to `LocalizationService`, fixing raw `[Common_Open]` bracket display in `BatchFolderPage`.
  - Localized Win2D canvas hub/empty text (`EmptyWheelText`, `CenterHubText`) and winner modal odds (`WinningProbabilityText`) in `LuckyWheelViewModel` and `LuckyWheelPage.xaml.cs`, and added Win2D resource disposal prior to recreation in `WheelCanvas_CreateResources`.
  - Explicitly bound `CancelCommand="{x:Bind ViewModel.CancelCurrentOperationCommand}"` on `StatusFooterControl` across all utility module pages (`BatchFolderPage`, `BatchRenamePage`, `ChecksumPage`, `FileToolsPage`, `TextInspectorPage`).
  - Fixed repository root path resolution (`$PSScriptRoot\..\..\..\..`) and excluded `bin`/`obj` folders across all 5 automated governance scripts in `.agents/skills/*/scripts/*.ps1`.
- **Build Infrastructure & MSB3026 File Lock Elimination**:
  - Created global MSBuild target `Directory.Build.targets` with `KillLockingProcessesBeforeBuild`.
  - Automatically terminates lingering instances of `Microsoft.UI.Xaml.Markup.Compiler.exe` and running `OmniForge.App.exe` across all projects prior to compilation.
  - Completely resolved `warning MSB3026` / `error MSB3027` ("Could not copy obj/.../OmniForge.Core.dll to bin/.../OmniForge.Core.dll because it is being used by another process: Microsoft.UI.Xaml.Markup.Compiler").
- **Lucky Wheel Weight Stepper Direct Numeric Input & Fixed Range Clamping**:
  - Replaced read-only `TextBlock` in `LuckyWheelPage.xaml` weight stepper with interactive `TextBox` supporting direct numeric input, Enter key commit via `WeightTextBox_KeyDown`, and two-way binding.
  - Constrained weight values to a fixed scale of `[0.0, 10.0]` (clamping typed numbers, increment/decrement buttons, and quick-add default inputs).
  - Enhanced parsing in `WheelItemViewModel` to support comma or period decimals (`5,5` or `5.5`), negative numbers clamped to `0.0`, and percentage suffixes (`10%`).
  - Widened the table column width from 92px to 96px for comfortable numeric editing without truncation.
  - Centered table column header `Weight` and vertically centered number text inside the stepper pill with `Padding="0,4,0,0"`, eliminating text riding the top border.
- **Lucky Wheel Table Alignment & Zero-CLS Layout Shift Elimination**:
  - Embedded the column header directly inside `ListView.Header`, locking horizontal alignment and column widths (`24px`, `*`, `96px`, `60px`, `32px`, `32px`) synchronously with row items even when the vertical scrollbar appears.
  - Added explicit zero-padding `ItemContainerStyle` to `ListViewItem`, eliminating internal indentation offset and hover misalignment against the header.
  - Replaced elastic `MinHeight="220"` / `MaxHeight="360"` with fixed `Height="340"` container, eliminating vertical layout shifting (CLS) and scrollbar popping when adding, removing, or filtering options.
  - Lowered `WideState` responsive breakpoint from 1020px to 900px to maintain stable two-column layout on standard desktop windows.
- **Batch Folder Preview Panel Layout**:
  - Restructured `BatchFolderPage.xaml` PreviewCard header layout. Separated title/view toggles, status metric badges (`Total`, `New`, `Exists`, `Errors`), and search filter into dedicated rows. Resolved severe vertical word-wrap and character squishing ("📁 Xe / Phân / cấp / thư / mục...") caused by competing `Auto` and `*` columns.
- **Dashboard Quick Launch Card Layout & Category Badge**:
  - Restructured `DashboardPage.xaml` utility cards by elevating the category badge to an eyebrow tag above the tool title. Increased `ItemsWrapGrid` `ItemWidth` from 360px to 370px and tightened layout spacing. Eliminated category tag truncation and clipping (`[D]`, `[Devel]`, `[File Syst]`, `[File S]`, `[Se]`) while granting 100% full width to Vietnamese and English module titles.

### Added
- **UI/UX Architecture Standardization & Fluent Design System Overhaul**:
  - **Standardized Semantic Tokens**: Added spacing scale tokens (`SpaceInsetMicro`, `SpaceInsetSmall`, `SpaceInsetMedium`, `SpaceInsetLarge`) and corner radius tokens (`ControlCornerRadiusSmall`, `ControlCornerRadiusMedium`, `CardCornerRadiusLarge`) in `SharedStyles.xaml` for consistent visual hierarchy and zero-hardcoded styling.
  - **Waffle Menu App Launcher**: Integrated quick App Launcher (`&#xED35;`) in `MainWindow.xaml` pane header, allowing users to jump directly to any utility module from anywhere in the app with localized descriptions.
  - **Batch Rename Progressive Disclosure**: Replaced flat delete buttons with The Three Dots (`&#xE712;`) overflow flyout on preview rows (supporting Copy Path, Open in File Explorer, and Remove), and upgraded the primary action to a `SplitButton` with instant execution and undo options.
  - **Checksum Export & Quick Actions**: Added `DropDownButton` on Checksum cryptographic digest card with options to copy lowercase hex, UPPERCASE HEX, or full formatted verification reports.
  - **Dashboard Category Filters & Onboarding Tour**: Added interactive category filter buttons (`All Tools`, `File Operations`, `General Utilities`) and a native `TeachingTip` coach-marks tour for quick search discovery and keyboard shortcuts.
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

