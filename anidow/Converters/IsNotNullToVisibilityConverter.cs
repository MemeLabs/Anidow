using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Anidow.Converters
{
    public class IsNotNullToVisibilityConverter : IValueConverter
    {
        public static readonly IsNotNullToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value == null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}