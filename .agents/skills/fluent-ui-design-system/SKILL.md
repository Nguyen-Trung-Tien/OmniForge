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

## 3. Standard Card Container Snippet

```xml
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        CornerRadius="8"
        Padding="20">
    <StackPanel Spacing="12">
        <!-- Card Content -->
    </StackPanel>
</Border>
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

