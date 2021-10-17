using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Anidow.Converters;

internal class IsStringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public static readonly IsStringNullOrEmptyToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null or not string)
        {
            return Visibility.Collapsed;
        }

        return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}