using System;
using System.Globalization;
using System.Windows.Data;

namespace Anidow.Converters
{
    public class DownloadCoverConverter : IMultiValueConverter
    {
        public static readonly DownloadCoverConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            (values[0], values[1]);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}