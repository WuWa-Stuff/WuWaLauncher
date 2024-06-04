using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WuWaTranslated.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        Visibility whenFalse = Visibility.Collapsed;
        if (parameter is string vParam)
            whenFalse = vParam.Equals("Hidden", StringComparison.OrdinalIgnoreCase) ? Visibility.Hidden : whenFalse;

        if (value is bool b)
            return b ? Visibility.Visible : whenFalse;

        throw new NotSupportedException("This converter supports only Visibility<->bool types conversion!");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        if (value is Visibility visibility)
            return visibility is Visibility.Visible;

        throw new NotSupportedException("This converter supports only Visibility<->bool types conversion!");
    }
}