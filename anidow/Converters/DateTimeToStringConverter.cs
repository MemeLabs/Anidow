using Humanizer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Anidow.Converters
{
    class DateTimeToStringConverter : IValueConverter
    {
        public static readonly DateTimeToStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = ((DateTime)value!).ToLocalTime().Humanize();
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value;
    }
}
