using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace OmniForge.App.Modules.LuckyWheel;

/// <summary>
/// Observable presentation model for historical spin victories.
/// </summary>
public partial class WinnerHistoryEntryViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Weight { get; set; } = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedOdds))]
    public partial double Percentage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    [NotifyPropertyChangedFor(nameof(ContrastForegroundBrush))]
    public partial string ColorHex { get; set; } = "#2563EB";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTime))]
    public partial DateTime WonAt { get; set; } = DateTime.Now;

    public string FormattedTime => WonAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public string FormattedOdds => $"{Percentage.ToString("F1", CultureInfo.InvariantCulture)}%";

    public SolidColorBrush Brush => new(WheelItemViewModel.ParseHexColor(ColorHex));

    public SolidColorBrush ContrastForegroundBrush =>
        WheelItemViewModel.IsLightColor(WheelItemViewModel.ParseHexColor(ColorHex))
            ? new SolidColorBrush(Colors.Black)
            : new SolidColorBrush(Colors.White);

    public WinnerHistoryEntryViewModel() { }

    public WinnerHistoryEntryViewModel(string name, double weight, double percentage, string colorHex, DateTime wonAt)
    {
        Name = name;
        Weight = weight;
        Percentage = percentage;
        ColorHex = colorHex;
        WonAt = wonAt;
    }
}
