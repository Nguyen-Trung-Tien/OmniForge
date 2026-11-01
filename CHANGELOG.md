# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Responsive Adaptive Layouts**: Added `VisualStateManager` with `AdaptiveTrigger` (`MinWindowWidth="980"` vs `MinWindowWidth="0"`) across `DashboardPage`, `BatchFolderPage`, `BatchRenamePage`, `TextInspectorPage`, `LuckyWheelPage`, `FileToolsPage`, `ChecksumPage`, and `SettingsPage`, plus `PaneDisplayMode="Auto"` (`ExpandedModeThresholdWidth="960"`, `CompactModeThresholdWidth="640"`) and minimum window bounds (`680x500`) in `MainWindow`.
- **Batch Folder Creator (Visual Row & Series Builder)**: Added Quick-Add Folder Row bar (`QuickAddFolderCommand`, `Subfolder` toggle, Enter key shortcut), interactive `Series...` Flyout generator (`FolderParser.GenerateSeriesText`), Indent/Outdent (`AdjustIndentation`), Clean/Sort (`NormalizeLines`), synchronized Line Numbers gutter (`LineNumbersText`), Expand/Collapse All tree controls, and inline `ExecutionResultSummary` `InfoBar`.
- **Batch Rename**: Added 1-click Quick Rule Chips (`+ Prefix`, `+ Replace`, `+ Case`, `+ Number`), `IncludeSubfolders` toggle, `ClearRulesCommand`, empty rules placeholder, and inline `ValidationError` & status icon visibility directly inside preview rows.
- **File Utilities**: Added `IncludeSubfolders` scan toggle, labeled toolbar buttons, and per-row `CopyFilePathCommand` and `RevealFileInExplorerCommand` (`explorer.exe /select`) actions.
- **Lucky Wheel**: Added table column headers (`Option Label`, `Weight`, `Odds`, `On`), `EqualizeWeightsCommand`, `ClearItemsCommand`, `EmptyStateControl` when options are empty, and `StatusFooterControl`.
- **Text & Payload Inspector**: Added side-by-side Split-View workspace, live character/line counters (`InputStatsText`, `OutputStatsText`), 1-click `Sample` payload loader across all 6 modes, `Swap ⇄` Input/Output command, and semantic Green (`+`) / Red (`-`) coloring with `EmptyStateControl` in Diff mode.
- **Checksum & Hasher**: Added full-page Drag & Drop support, automatic hash recomputation on algorithm switch, instant automatic hash comparison on paste/edit, `PasteExpectedHashCommand`, and semantic `InfoBar` verification banners.
- **Dashboard**: Surfaced `SystemStatus` and `FoundationHighlights` overview cards alongside the real-time utility search filter bar and responsive `ItemsWrapGrid` tool launcher cards.

### Changed
- **State Persistence**: Changed discovered tool ViewModels in `ModuleAutoDiscoveryExtensions` from `Transient` to `Singleton` so active work is preserved when switching between utilities.
- **Visual Hierarchy**: Streamlined `DashboardPage`, `TextInspectorPage`, `ChecksumPage`, `BatchFolderPage`, and `FileToolsPage` by anchoring `StatusFooterControl` at the bottom of the workspace and replacing hardcoded XAML colors with Fluent semantic theme brushes.

### Fixed
- **Shell Navigation**: Fixed unexpected page jumps when typing in the sidebar `AutoSuggestBox` search filter and removed duplicate `ItemInvoked`/`SelectionChanged` navigation triggers in `MainWindow`.
- **Error Banner Dismissal**: Fixed `ToolHeaderControl` `InfoBar` close behavior with two-way `HasError`/`ErrorMessage` synchronization so subsequent errors reliably re-open the error banner.
- **Batch Folder Editor**: Fixed line-number scroll desynchronization and rigid column sizing in `BatchFolderPage`.
- **File Utilities I/O**: Fixed default `UserProfile` recursive directory scan bottleneck by defaulting to `MyDocuments` and computing size directly from enumerated metadata.

