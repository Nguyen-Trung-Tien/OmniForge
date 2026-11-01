---
name: fluent-ui-design-system
description: Compact cheat-sheet of WinUI 3 Fluent Design theme brushes, typography styles, card containers, and Segoe Fluent Icons to prevent hardcoded colors and verbose XAML.
---

# WinUI 3 Fluent UI Design System Skill

Use this skill whenever authoring or styling XAML pages and controls in **OmniForge**. Never hardcode hex colors (`#FFFFFF`, `#1E1E1E`)—always reference native WinUI 3 theme resources.

---

## 1. Directory Structure & Assets

- Audit Script: [audit_theme_resources.ps1](scripts/audit_theme_resources.ps1)
- Reference Page: [FluentStandardPageExample.xaml](examples/FluentStandardPageExample.xaml)
- Theme Brushes Catalog: [fluent-theme-brushes.json](resources/fluent-theme-brushes.json)
- Segoe Icons Catalog: [segoe-fluent-icons.json](resources/segoe-fluent-icons.json)

---

## 2. Standard Theme Brushes (Light / Dark / High-Contrast Safe)

| Purpose | ThemeResource Key |
| :--- | :--- |
| Page background | `{ThemeResource ApplicationPageBackgroundThemeBrush}` |
| Card / elevated surface | `{ThemeResource CardBackgroundFillColorDefaultBrush}` |
| Secondary card surface | `{ThemeResource CardBackgroundFillColorSecondaryBrush}` |
| Card border stroke | `{ThemeResource CardStrokeColorDefaultBrush}` |
| Primary body text | `{ThemeResource TextFillColorPrimaryBrush}` |
| Secondary / muted text | `{ThemeResource TextFillColorSecondaryBrush}` |
| Accent highlight text | `{ThemeResource AccentTextFillColorPrimaryBrush}` |
| Success status text | `{ThemeResource SystemFillColorSuccessBrush}` |
| Warning status text | `{ThemeResource SystemFillColorCautionBrush}` |
| Critical error text | `{ThemeResource SystemFillColorCriticalBrush}` |

---

## 3. Standard Typography Styles

- **Page Title**: `Style="{ThemeResource TitleTextBlockStyle}"` (or use `<controls:ToolHeaderControl>`)
- **Section Header**: `Style="{ThemeResource SubtitleTextBlockStyle}"`
- **Card Header / Bold Label**: `Style="{ThemeResource BodyStrongTextBlockStyle}"`
- **Standard Description**: `Style="{ThemeResource BodyTextBlockStyle}"`
- **Metadata / Status Bar**: `Style="{ThemeResource CaptionTextBlockStyle}"`

---

## 4. Reusable Custom Controls (`xmlns:controls="using:OmniForge.App.Controls"`)

- `<controls:ToolHeaderControl>` — Page title, subtitle, and fault-isolated `InfoBar`
- `<controls:SectionCardControl>` — Elevated Fluent card container
- `<controls:StatusFooterControl>` — Progress bar, status text, and execution duration badge
- `<controls:EmptyStateControl>` — Placeholder when data is empty

See [FluentStandardPageExample.xaml](examples/FluentStandardPageExample.xaml) for a complete reference layout.
