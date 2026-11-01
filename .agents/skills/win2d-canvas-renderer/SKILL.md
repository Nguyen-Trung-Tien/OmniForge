---
name: win2d-canvas-renderer
description: Performance guide, device resource management patterns, and DPI awareness guidelines for high-FPS rendering with Win2D CanvasControl in WinUI 3.
---

# Win2D Canvas Renderer Skill

This skill provides architecture patterns, rendering lifecycle guidelines, and high-performance practices for integrating Microsoft Win2D (`Microsoft.Graphics.Win2D`) into WinUI 3 tools within OmniForge.

---

## 1. Directory Structure & Assets

- Diagnostic Script: [verify_win2d_support.ps1](scripts/verify_win2d_support.ps1)
- Reference Control: [RotatingCanvasExample.cs](examples/RotatingCanvasExample.cs)
- Easing Curves Catalog: [easing-curves.json](resources/easing-curves.json)

---

## 2. Win2D Rendering Controls Overview

| Control | Use Case | Rendering Thread |
| :--- | :--- | :--- |
| `CanvasControl` | Immediate-mode 2D drawing, vector graphics, diagrams, static or event-driven redraws | UI Thread (`Draw` event) |
| `CanvasAnimatedControl` | High-FPS game loops, physics simulations, 60+ FPS real-time visualizations | Dedicated Render Thread (`Update` / `Draw` events) |
| `CanvasVirtualControl` | Extremely large canvases (pan/zoom image viewports, infinite whiteboards) | UI Thread on demand |

---

## 2. Resource Lifecycle & Device Management

Win2D graphics resources are tied to a DirectX Graphics Device (`CanvasDevice`). If the GPU driver restarts, the window moves between disparate GPUs, or the display enters sleep, device resources become invalidated.

### 2.1 The Two Resource Categories
1. **Device-Independent Resources**:
   - Geometries (`CanvasGeometry`), color values, layout matrices.
   - These survive device loss and can be cached long-term.
2. **Device-Dependent Resources**:
   - Bitmaps (`CanvasBitmap`), command lists (`CanvasCommandList`), brushes with image shaders, effect graphs (`GaussianBlurEffect`).
   - Must be loaded or allocated exclusively inside `CreateResources`.

### 2.2 Standard `CreateResources` Pattern
```csharp
private CanvasBitmap? _cachedBitmap;
private CanvasSolidColorBrush? _accentBrush;

private void OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
{
    // Recreate device resources when device is re-initialized
    args.TrackAsyncAction(Task.Run(async () =>
    {
        // Load asynchronous image assets or compute-heavy surfaces
        var bitmap = await CanvasBitmap.LoadAsync(sender.Device, "Assets/Texture.png");
        _cachedBitmap = bitmap;
    }).AsAsyncAction());
}
```

---

## 3. High-FPS Rendering & DPI Awareness

### 3.1 DPI-Independent Coordinate Systems
- WinUI 3 and Win2D operate with Device Independent Pixels (DIPs) by default (96 DIPs = 1 inch).
- `sender.Dpi` provides the current display DPI.
- Never hardcode physical pixel coordinates when drawing geometry, text, or shapes.
- Use `sender.ConvertDipsToPixels` and `sender.ConvertPixelsToDips` only when transferring raw pixel buffers (e.g. software render targets or byte arrays).

### 3.2 High-FPS Best Practices
1. **Minimize Allocations in `Draw`**:
   - Never instantiate `new CanvasSolidColorBrush(...)`, `new CanvasGeometry(...)`, or allocate arrays inside the `Draw` event handler.
   - Cache brushes, colors, and reusable geometries during `CreateResources` or ViewModel initialization.
2. **Draw Session Batching**:
   - Group draw calls that share state.
   - Prefer `DrawGeometry` and `FillGeometry` with pre-compiled paths over thousands of individual `DrawLine` calls.
3. **Double Buffering & Invalidation**:
   - In `CanvasControl`, call `sender.Invalidate()` only when visual state has actually changed.
   - For continuous 60 FPS rendering, prefer `CanvasAnimatedControl` with `IsFixedTimeStep = true` and `TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0)`.

---

## 4. Production Example: Win2D Canvas Host Page

### XAML (`CanvasVisualizerPage.xaml`):
```xml
<Page
    x:Class="OmniForge.App.Modules.Visualizer.CanvasVisualizerPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:canvas="using:Microsoft.Graphics.Canvas.UI.Xaml">

    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <canvas:CanvasControl x:Name="Canvas"
                              ClearColor="Transparent"
                              CreateResources="Canvas_CreateResources"
                              Draw="Canvas_Draw" />
    </Grid>
</Page>
```

### Code-Behind (`CanvasVisualizerPage.xaml.cs`):
```csharp
using System.Numerics;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.Visualizer;

public sealed partial class CanvasVisualizerPage : Page
{
    private CanvasSolidColorBrush? _waveBrush;

    public CanvasVisualizerPage()
    {
        InitializeComponent();
    }

    private void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Allocate device-dependent brush
        _waveBrush = new CanvasSolidColorBrush(sender.Device, Colors.DodgerBlue);
    }

    private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (_waveBrush is null) return;

        using var ds = args.DrawingSession;
        float width = (float)sender.ActualWidth;
        float height = (float)sender.ActualHeight;

        // Draw antialiased sine wave visualization
        float centerY = height / 2.0f;
        Vector2? previousPoint = null;

        for (float x = 0; x < width; x += 4f)
        {
            float y = centerY + MathF.Sin(x * 0.02f) * 40.0f;
            var currentPoint = new Vector2(x, y);

            if (previousPoint.HasValue)
            {
                ds.DrawLine(previousPoint.Value, currentPoint, _waveBrush, 2.5f);
            }

            previousPoint = currentPoint;
        }
    }
}
```

