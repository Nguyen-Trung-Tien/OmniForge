using System.Numerics;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;

namespace OmniForge.App.Modules.Visualizer;

/// <summary>
/// Production reference example demonstrating 60 FPS hardware-accelerated Win2D rendering
/// with cached geometry, device-dependent resources, and DPI-aware coordinate transformations.
/// </summary>
public sealed class RotatingCanvasExample : UserControl
{
    private readonly CanvasAnimatedControl _animatedCanvas;
    private CanvasSolidColorBrush? _primaryBrush;
    private CanvasSolidColorBrush? _accentBrush;
    private CanvasGeometry? _cachedPolygon;
    private float _rotationAngle;

    public RotatingCanvasExample()
    {
        _animatedCanvas = new CanvasAnimatedControl
        {
            IsFixedTimeStep = true,
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0), // Lock to 60 FPS
            ClearColor = Colors.Transparent
        };

        _animatedCanvas.CreateResources += OnCreateResources;
        _animatedCanvas.Update += OnUpdate;
        _animatedCanvas.Draw += OnDraw;

        Content = _animatedCanvas;
    }

    private void OnCreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
    {
        // 1. Allocate device-dependent brushes
        _primaryBrush = new CanvasSolidColorBrush(sender.Device, Colors.DodgerBlue);
        _accentBrush = new CanvasSolidColorBrush(sender.Device, Colors.Cyan);

        // 2. Pre-compile reusable geometry (avoiding per-frame allocations)
        Vector2[] points =
        [
            new Vector2(0, -60),
            new Vector2(50, 30),
            new Vector2(-50, 30)
        ];

        _cachedPolygon = CanvasGeometry.CreatePolygon(sender.Device, points);
    }

    private void OnUpdate(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
    {
        // Increment rotation based on frame delta time
        float delta = (float)args.Timing.ElapsedTime.TotalSeconds;
        _rotationAngle += delta * 1.5f; // ~1.5 radians per second
        if (_rotationAngle > MathF.PI * 2)
        {
            _rotationAngle -= MathF.PI * 2;
        }
    }

    private void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        if (_cachedPolygon is null || _primaryBrush is null || _accentBrush is null) return;

        using var ds = args.DrawingSession;
        var center = new Vector2((float)sender.Size.Width / 2f, (float)sender.Size.Height / 2f);

        // Apply rotation transform around center of canvas
        var transform = Matrix3x2.CreateRotation(_rotationAngle, Vector2.Zero) *
                        Matrix3x2.CreateTranslation(center);

        ds.Transform = transform;

        // Draw filled polygon and high-contrast outline
        ds.FillGeometry(_cachedPolygon, _primaryBrush);
        ds.DrawGeometry(_cachedPolygon, _accentBrush, strokeWidth: 3.0f);
    }
}

