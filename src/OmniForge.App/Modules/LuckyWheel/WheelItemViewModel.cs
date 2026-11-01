using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using OmniForge.Core.Modules.LuckyWheel;
using Windows.UI;

namespace OmniForge.App.Modules.LuckyWheel;

/// <summary>
/// Observable ViewModel representing a single interactive item on the Lucky Wheel.
/// Handles reactive color swatch mapping, contrast calculation, and weight editing.
/// </summary>
public partial class WheelItemViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    private double _weight = 1.0;

    public double Weight
    {
        get => _weight;
        set
        {
            double sanitized = double.IsNaN(value) || double.IsInfinity(value)
                ? 1.0
                : Math.Round(Math.Clamp(value, 0.0, 10.0), 2);

            if (SetProperty(ref _weight, sanitized))
            {
                OnPropertyChanged(nameof(WeightText));
                OnPropertyChanged(nameof(PercentageText));
            }
        }
    }

    public string WeightText
    {
        get => Weight.ToString("0.##", CultureInfo.InvariantCulture);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                OnPropertyChanged(nameof(WeightText));
                return;
            }

            string normalized = value.Trim().TrimEnd('%').Trim().Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                Weight = parsed;
            }
            else
            {
                OnPropertyChanged(nameof(WeightText));
            }
        }
    }

    [RelayCommand]
    public void IncreaseWeight()
    {
        Weight = Math.Round(Math.Min(10.0, Weight + 0.5), 2);
    }

    [RelayCommand]
    public void DecreaseWeight()
    {
        Weight = Math.Round(Math.Max(0.0, Weight - 0.5), 2);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowsColor))]
    [NotifyPropertyChangedFor(nameof(Brush))]
    [NotifyPropertyChangedFor(nameof(ContrastForegroundBrush))]
    public partial string ColorHex { get; set; } = "#2563EB";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PercentageText))]
    public partial double Percentage { get; set; }

    public string PercentageText => $"{Percentage:F1}%";

    public Color WindowsColor => ParseHexColor(ColorHex);

    public SolidColorBrush Brush => new(WindowsColor);

    public SolidColorBrush ContrastForegroundBrush =>
        IsLightColor(WindowsColor) ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White);

    public WheelItemViewModel() { }

    public WheelItemViewModel(WheelItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Id = item.Id;
        Text = item.Text;
        Weight = Math.Clamp(item.Weight, 0.0, 10.0);
        ColorHex = item.Color;
        IsEnabled = item.IsEnabled;
    }

    public WheelItem ToModel() => new()
    {
        Id = Id,
        Text = Text,
        Weight = Math.Max(0.01, Weight),
        Color = ColorHex,
        IsEnabled = IsEnabled
    };

    public WheelItemViewModel Clone() => new()
    {
        Id = Guid.NewGuid(),
        Text = Text,
        Weight = Weight,
        ColorHex = ColorHex,
        IsEnabled = IsEnabled,
        Percentage = Percentage
    };

    public static Color ParseHexColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Color.FromArgb(255, 37, 99, 235);
        }

        string clean = hex.Trim().TrimStart('#');
        if (clean.Length == 6 && uint.TryParse(clean, System.Globalization.NumberStyles.HexNumber, null, out uint rgb))
        {
            return Color.FromArgb(255, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
        }

        if (clean.Length == 8 && uint.TryParse(clean, System.Globalization.NumberStyles.HexNumber, null, out uint argb))
        {
            return Color.FromArgb((byte)((argb >> 24) & 0xFF), (byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
        }

        return Color.FromArgb(255, 37, 99, 235);
    }

    public static bool IsLightColor(Color c)
    {
        double luminance = (0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B);
        return luminance > 140.0;
    }
}
