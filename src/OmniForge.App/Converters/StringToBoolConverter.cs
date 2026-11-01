using Microsoft.UI.Xaml.Data;

namespace OmniForge.App.Converters;

/// <summary>
/// Converts a string value to a boolean indicating whether the string is non-null and non-whitespace.
/// </summary>
public sealed class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool hasText = value is string text && !string.IsNullOrWhiteSpace(text);
        if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            return !hasText;
        }

        return hasText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException("StringToBoolConverter supports OneWay binding only.");
    }
}

