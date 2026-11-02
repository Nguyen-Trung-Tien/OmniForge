using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OmniForge.Core.Modules.LuckyWheel;
using Windows.UI;

namespace OmniForge.App.Modules.LuckyWheel;

public sealed partial class LuckyWheelPage : Page, IDisposable
{
    public LuckyWheelViewModel ViewModel { get; } = App.GetService<LuckyWheelViewModel>();

    private CanvasTextFormat? _labelFormat;
    private CanvasTextFormat? _centerHubFormat;

    // Ticker Spring Physics
    private float _tickerAngle;
    private float _tickerVelocity;
    private const float SpringConstant = 320.0f;
    private const float DampingCoefficient = 18.0f;

    // Confetti System
    private readonly List<ConfettiParticle> _confetti = [];
    private DateTime _lastFrameTime = DateTime.UtcNow;
    private bool _isRenderLoopActive;

    public LuckyWheelPage()
    {
        DataContext = ViewModel;
        InitializeComponent();

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.WheelAngleUpdated += OnWheelAngleUpdated;
        ViewModel.DividerTickOccurred += OnDividerTickOccurred;
        ViewModel.SpinCompleted += OnSpinCompleted;

        StartRenderLoop();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        StopRenderLoop();

        ViewModel.WheelAngleUpdated -= OnWheelAngleUpdated;
        ViewModel.DividerTickOccurred -= OnDividerTickOccurred;
        ViewModel.SpinCompleted -= OnSpinCompleted;

        _labelFormat?.Dispose();
        _centerHubFormat?.Dispose();
        _labelFormat = null;
        _centerHubFormat = null;
    }

    private void StartRenderLoop()
    {
        if (_isRenderLoopActive) return;
        _isRenderLoopActive = true;
        _lastFrameTime = DateTime.UtcNow;
        CompositionTarget.Rendering += OnRenderingFrame;
    }

    private void StopRenderLoop()
    {
        if (!_isRenderLoopActive) return;
        _isRenderLoopActive = false;
        CompositionTarget.Rendering -= OnRenderingFrame;
    }

    private void OnRenderingFrame(object? sender, object e)
    {
        var now = DateTime.UtcNow;
        double dt = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        // Clamp delta time to avoid physics explosions during pauses
        dt = Math.Clamp(dt, 0.001, 0.05);

        bool needsRedraw = false;

        // 1. Advance wheel physics if spinning
        if (ViewModel.IsSpinning)
        {
            ViewModel.AdvancePhysicsStep(dt);
            needsRedraw = true;
        }

        // 2. Advance pointer ticker spring-damper physics
        if (Math.Abs(_tickerAngle) > 0.001f || Math.Abs(_tickerVelocity) > 0.001f)
        {
            float springForce = -SpringConstant * _tickerAngle;
            float dampingForce = -DampingCoefficient * _tickerVelocity;
            float totalAcceleration = springForce + dampingForce;

            _tickerVelocity += totalAcceleration * (float)dt;
            _tickerAngle += _tickerVelocity * (float)dt;

            if (Math.Abs(_tickerAngle) < 0.001f && Math.Abs(_tickerVelocity) < 0.005f)
            {
                _tickerAngle = 0.0f;
                _tickerVelocity = 0.0f;
            }

            needsRedraw = true;
        }

        // 3. Update celebratory confetti
        if (_confetti.Count > 0)
        {
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var p = _confetti[i];
                p.Position += p.Velocity * (float)dt;
                p.Velocity.Y += 380.0f * (float)dt; // Gravity
                p.Angle += p.AngularVelocity * (float)dt;
                p.Life -= (float)dt * p.Decay;
                p.Opacity = Math.Clamp(p.Life, 0.0f, 1.0f);

                if (p.Life <= 0.0f)
                {
                    _confetti.RemoveAt(i);
                }
            }
            needsRedraw = true;
        }

        if (needsRedraw)
        {
            WheelCanvas.Invalidate();
        }
    }

    private void OnWheelAngleUpdated(double angle)
    {
        WheelCanvas.Invalidate();
    }

    private void OnDividerTickOccurred()
    {
        // Deflect pointer needle counter-clockwise as divider pin pushes it
        _tickerAngle = -0.32f;
        _tickerVelocity = 4.0f;
        StartRenderLoop();
    }

    private void OnSpinCompleted(WheelItemViewModel winner)
    {
        if (ViewModel.IsConfettiEnabled)
        {
            SpawnConfettiBurst();
        }
        StartRenderLoop();
    }

    private void SpawnConfettiBurst()
    {
        _confetti.Clear();
        float width = (float)WheelCanvas.ActualWidth;
        float height = (float)WheelCanvas.ActualHeight;
        Vector2 center = new(width / 2.0f, height / 2.0f);

        var rng = Random.Shared;
        for (int i = 0; i < 75; i++)
        {
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            float speed = 180.0f + (float)(rng.NextDouble() * 340.0);
            var colorHex = LuckyWheelService.DefaultPalette[i % LuckyWheelService.DefaultPalette.Length];

            _confetti.Add(new ConfettiParticle
            {
                Position = center,
                Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed - 150.0f),
                Angle = (float)(rng.NextDouble() * Math.PI),
                AngularVelocity = (float)((rng.NextDouble() - 0.5) * 12.0),
                Size = new Vector2(6.0f + (float)(rng.NextDouble() * 6.0), 10.0f + (float)(rng.NextDouble() * 8.0)),
                Color = WheelItemViewModel.ParseHexColor(colorHex),
                Life = 1.0f,
                Decay = 0.35f + (float)(rng.NextDouble() * 0.25),
                Opacity = 1.0f
            });
        }
    }

    private void WheelCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        _labelFormat?.Dispose();
        _centerHubFormat?.Dispose();

        _labelFormat = new CanvasTextFormat
        {
            FontFamily = "Segoe UI",
            FontWeight = FontWeights.SemiBold,
            FontSize = 13.5f,
            HorizontalAlignment = CanvasHorizontalAlignment.Right,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            WordWrapping = CanvasWordWrapping.NoWrap,
            TrimmingGranularity = CanvasTextTrimmingGranularity.Character
        };

        _centerHubFormat = new CanvasTextFormat
        {
            FontFamily = "Segoe UI",
            FontWeight = FontWeights.ExtraBold,
            FontSize = 15.0f,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center
        };
    }

    private void WheelCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (_labelFormat is null || _centerHubFormat is null) return;

        using var ds = args.DrawingSession;
        float width = (float)sender.ActualWidth;
        float height = (float)sender.ActualHeight;

        if (width < 60f || height < 60f) return;

        Vector2 center = new(width / 2.0f, height / 2.0f);
        float outerRadius = Math.Min(width, height) * 0.44f;
        var slices = ViewModel.Slices;

        // 1. Outer Drop Shadow & Metallic Rim
        ds.DrawCircle(center, outerRadius + 8f, Color.FromArgb(40, 0, 0, 0), 12.0f);
        ds.DrawCircle(center, outerRadius + 4f, Color.FromArgb(255, 30, 41, 59), 8.0f);
        ds.DrawCircle(center, outerRadius, Color.FromArgb(255, 71, 85, 105), 2.0f);

        if (slices.Count == 0)
        {
            ds.DrawText(ViewModel.EmptyWheelText, center, Color.FromArgb(160, 150, 150, 150), _centerHubFormat);
            return;
        }

        var origTransform = ds.Transform;

        // 2. Rotating Wheel Slices & Border Studs
        ds.Transform = Matrix3x2.CreateRotation((float)ViewModel.CurrentAngle, center) * origTransform;

        for (int i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            Color sliceColor = WheelItemViewModel.ParseHexColor(slice.Item.Color);

            using var path = new CanvasPathBuilder(sender.Device);
            path.BeginFigure(center);
            path.AddArc(center, outerRadius, outerRadius, (float)slice.StartAngle, (float)slice.SweepAngle);
            path.EndFigure(CanvasFigureLoop.Closed);
            using var geometry = CanvasGeometry.CreatePath(path);

            ds.FillGeometry(geometry, sliceColor);
            ds.DrawGeometry(geometry, Color.FromArgb(100, 255, 255, 255), 1.5f);

            // Divider Outer Pin / Stud
            Vector2 studPos = center + new Vector2(
                (outerRadius - 2f) * MathF.Cos((float)slice.EndAngle),
                (outerRadius - 2f) * MathF.Sin((float)slice.EndAngle));

            ds.FillCircle(studPos, 3.5f, Color.FromArgb(255, 245, 158, 11)); // Amber Gold Stud
            ds.DrawCircle(studPos, 3.5f, Color.FromArgb(255, 255, 255, 255), 1.0f);
        }

        // 3. Radial Slice Labels
        for (int i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            if (slice.SweepAngle < 0.08f) continue;

            float midAngle = (float)slice.MidAngle;

            ds.Transform = Matrix3x2.CreateRotation(midAngle, center) *
                           Matrix3x2.CreateRotation((float)ViewModel.CurrentAngle, center) *
                           origTransform;

            Color sliceColor = WheelItemViewModel.ParseHexColor(slice.Item.Color);
            Color labelColor = WheelItemViewModel.IsLightColor(sliceColor)
                ? Color.FromArgb(255, 15, 23, 42)
                : Color.FromArgb(255, 255, 255, 255);

            float labelX = center.X + (outerRadius * 0.84f);
            float labelY = center.Y;

            ds.DrawText(slice.Item.Text, new Vector2(labelX, labelY), labelColor, _labelFormat);
        }

        ds.Transform = origTransform;

        // 4. Static Center Hub
        float hubOuterR = outerRadius * 0.22f;
        float hubInnerR = outerRadius * 0.17f;

        ds.FillCircle(center, hubOuterR, Color.FromArgb(255, 15, 23, 42));
        ds.DrawCircle(center, hubOuterR, Color.FromArgb(255, 71, 85, 105), 3.0f);

        Color hubButtonColor = ViewModel.IsSpinning
            ? Color.FromArgb(255, 225, 29, 72)
            : Color.FromArgb(255, 37, 99, 235);

        ds.FillCircle(center, hubInnerR, hubButtonColor);
        ds.DrawCircle(center, hubInnerR, Color.FromArgb(200, 255, 255, 255), 1.5f);

        ds.DrawText(ViewModel.CenterHubText, center, Color.FromArgb(255, 255, 255, 255), _centerHubFormat);

        // 5. Top Pointer / Ticker Needle
        Vector2 tickerPivot = new(center.X, center.Y - outerRadius - 12f);
        ds.Transform = Matrix3x2.CreateRotation(_tickerAngle, tickerPivot) * origTransform;

        using (var needlePath = new CanvasPathBuilder(sender.Device))
        {
            needlePath.BeginFigure(new Vector2(center.X - 11f, center.Y - outerRadius - 16f));
            needlePath.AddLine(new Vector2(center.X + 11f, center.Y - outerRadius - 16f));
            needlePath.AddLine(new Vector2(center.X, center.Y - outerRadius + 8f));
            needlePath.EndFigure(CanvasFigureLoop.Closed);
            using var needleGeom = CanvasGeometry.CreatePath(needlePath);

            ds.FillGeometry(needleGeom, Color.FromArgb(255, 239, 68, 68));
            ds.DrawGeometry(needleGeom, Color.FromArgb(255, 255, 255, 255), 2.0f);
        }

        ds.FillCircle(tickerPivot, 4.0f, Color.FromArgb(255, 245, 158, 11));
        ds.DrawCircle(tickerPivot, 4.0f, Color.FromArgb(255, 255, 255, 255), 1.0f);

        ds.Transform = origTransform;

        // 6. Confetti
        foreach (var p in _confetti)
        {
            Color c = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);
            ds.Transform = Matrix3x2.CreateRotation(p.Angle, p.Position) * origTransform;
            ds.FillRoundedRectangle(new Windows.Foundation.Rect(p.Position.X - p.Size.X / 2, p.Position.Y - p.Size.Y / 2, p.Size.X, p.Size.Y), 2f, 2f, c);
        }

        ds.Transform = origTransform;
    }

    private void WheelCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(WheelCanvas).Position;
        float width = (float)WheelCanvas.ActualWidth;
        float height = (float)WheelCanvas.ActualHeight;
        Vector2 center = new(width / 2.0f, height / 2.0f);
        float outerRadius = Math.Min(width, height) * 0.44f;
        float hubRadius = outerRadius * 0.22f;

        float dx = (float)pt.X - center.X;
        float dy = (float)pt.Y - center.Y;
        if ((dx * dx) + (dy * dy) <= (hubRadius * hubRadius))
        {
            if (ViewModel.CanSpin)
            {
                ViewModel.StartSpinCommand.Execute(null);
            }
        }
    }

    private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WheelItemViewModel item })
        {
            ViewModel.DeleteItemCommand.Execute(item);
        }
    }

    private void PresetItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string presetName })
        {
            ViewModel.LoadPresetCommand.Execute(presetName);
        }
    }

    private void DurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: ComboBoxItem { Tag: string tag } } &&
            double.TryParse(tag, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double duration))
        {
            ViewModel.SpinDurationSeconds = duration;
        }
    }

    private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedIndex: int idx })
        {
            ViewModel.DecelerationProfile = idx == 1 ? DecelerationProfile.Exponential : DecelerationProfile.EaseOutCubic;
        }
    }

    private void NewItemTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.AddNewItemCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void WeightTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
            e.Handled = true;
        }
    }

    public void Dispose()
    {
        _labelFormat?.Dispose();
        _centerHubFormat?.Dispose();
        _labelFormat = null;
        _centerHubFormat = null;
    }

    private sealed class ConfettiParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Angle;
        public float AngularVelocity;
        public Vector2 Size;
        public Color Color;
        public float Life;
        public float Decay;
        public float Opacity;
    }
}
