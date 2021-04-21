using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Anidow.Converters
{
    class IsZeroToVisibilityConverter : IValueConverter
    {
        public static readonly IsZeroToVisibilityConverter Instance = new IsZeroToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null && (int)value == 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
