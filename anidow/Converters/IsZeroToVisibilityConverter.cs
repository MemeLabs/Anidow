using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Anidow.Converters
{
    internal class IsZeroToVisibilityConverter : IValueConverter
    {
        public static readonly IsZeroToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null && (int) value == 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    internal class IsNotZeroToVisibilityConverter : IValueConverter
    {
        public static readonly IsNotZeroToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null && (int) value == 0 ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}