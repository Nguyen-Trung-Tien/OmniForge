# Changelog

All notable changes to the **OmniForge Utility Suite** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
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

