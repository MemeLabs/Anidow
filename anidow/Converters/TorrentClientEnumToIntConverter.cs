using System;
using System.Globalization;
using System.Windows.Data;
using Anidow.Enums;

namespace Anidow.Converters
{
    public class TorrentClientEnumToIntConverter : IValueConverter
    {
        public static readonly TorrentClientEnumToIntConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int) value!;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            (TorrentClient) value!;
    }
}