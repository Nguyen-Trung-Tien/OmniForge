---
name: fluent-ui-design-system
description: Compact cheat-sheet of WinUI 3 Fluent Design theme brushes, typography styles, card containers, and Segoe Fluent Icons to prevent hardcoded colors and verbose XAML.
---

# WinUI 3 Fluent UI Design System Skill

Use this skill whenever authoring or styling XAML pages and controls in **OmniForge**. Never hardcode hex colors (`#FFFFFF`, `#1E1E1E`)—always reference native WinUI 3 theme resources.

---

## 1. Standard Theme Brushes (Light / Dark / High-Contrast Safe)

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

## 2. Standard Typography Styles

- **Page Title**: `Style="{ThemeResource TitleTextBlockStyle}"` (or use `<controls:ToolHeaderControl>`)
- **Section Header**: `Style="{ThemeResource SubtitleTextBlockStyle}"`
- **Card Header / Bold Label**: `Style="{ThemeResource BodyStrongTextBlockStyle}"`
- **Standard Description**: `Style="{ThemeResource BodyTextBlockStyle}"`
- **Metadata / Status Bar**: `Style="{ThemeResource CaptionTextBlockStyle}"`

---

## 3. Shared Styles & Reusable Custom Controls (`OmniForge.App.Controls`)

Never repeat raw `<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" ...>` or manual status bars across pages. Always use the shared styles from `Styles/SharedStyles.xaml` and controls from `OmniForge.App.Controls`:

### 3.1 Page Container & Shared Styles (`SharedStyles.xaml`)
- `<ScrollViewer Style="{StaticResource PageScrollViewerStyle}">`
- `<StackPanel Style="{StaticResource PageRootStackPanelStyle}">`
- `<Border Style="{StaticResource FluentCardBorderStyle}">`
- `<TextBox Style="{StaticResource MonospaceCodeTextBoxStyle}" />` (Cascadia Code / Consolas for JSON, XML, Hashes, Regex)

### 3.2 Reusable XAML Controls (`xmlns:controls="using:OmniForge.App.Controls"`)

1. **`<controls:ToolHeaderControl>`** — Page title, subtitle, and fault-isolated `InfoBar`:
   ```xml
   <controls:ToolHeaderControl
       Title="Tool Title"
       Subtitle="Concise description of utility."
       HasError="{x:Bind ViewModel.HasError, Mode=OneWay}"
       ErrorMessage="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />
   ```
2. **`<controls:SectionCardControl>`** — Elevated Fluent card with `Title`, `Subtitle`, `IconGlyph`, `HeaderAction`, and direct child content:
   ```xml
   <controls:SectionCardControl Title="Input Settings" Subtitle="Configure options" IconGlyph="&#xE8B7;">
       <StackPanel Spacing="12">
           <!-- Card body controls -->
       </StackPanel>
   </controls:SectionCardControl>
   ```
3. **`<controls:StatusFooterControl>`** — Progress bar, status text, and execution duration pill badge:
   ```xml
   <controls:StatusFooterControl
       IsBusy="{x:Bind ViewModel.IsBusy, Mode=OneWay}"
       IsIndeterminate="{x:Bind ViewModel.IsIndeterminate, Mode=OneWay}"
       ProgressPercent="{x:Bind ViewModel.ProgressPercent, Mode=OneWay}"
       StatusMessage="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
       DurationText="{x:Bind ViewModel.LastDurationText, Mode=OneWay}" />
   ```
4. **`<controls:EmptyStateControl>`** — Placeholder when a `ListView` or result grid is empty:
   ```xml
   <controls:EmptyStateControl
       IconGlyph="&#xE8B7;"
       Title="No items loaded"
       Subtitle="Select a folder or drop files to begin."
       IsEmpty="{x:Bind ViewModel.IsEmpty, Mode=OneWay}" />
   ```

---

## 4. Common Segoe Fluent Icons Reference

| Utility Domain | Glyph Code (C#) | Glyph Code (XAML) |
| :--- | :--- | :--- |
| Dashboard / Home | `\uE80F` | `&#xE80F;` |
| File System / Folder | `\uE8B7` | `&#xE8B7;` |
| Developer / Code | `\uE943` | `&#xE943;` |
| Security / Hash / Shield | `\uE8D7` | `&#xE8D7;` |
| Text / Diff / Document | `\uE8A5` | `&#xE8A5;` |
| Graphics / Image / Win2D | `\uEB9F` | `&#xEB9F;` |
| System / Hardware / Speed | `\uEC4A` | `&#xEC4A;` |
| Network / Globe | `\uE774` | `&#xE774;` |
| Settings / Gear | `\uE713` | `&#xE713;` |

