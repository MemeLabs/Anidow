using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;

namespace Anidow.Converters
{
    internal class DateTimeToStringConverter : IValueConverter
    {
        public static readonly DateTimeToStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = ((DateTime) value!).Humanize();
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value;
    }
}