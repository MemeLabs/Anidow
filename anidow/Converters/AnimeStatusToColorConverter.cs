using System;
using System.Globalization;
using System.Windows.Data;
using Anidow.Enums;

namespace Anidow.Converters
{
    public class AnimeStatusToColorConverter : IValueConverter
    {
        public static readonly AnimeStatusToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (AnimeStatus) value!;

            var color = status switch
            {
                AnimeStatus.Watching => "#43A047",
                AnimeStatus.Completed => "#039BE5",
                AnimeStatus.Dropped => "#E53935",
                _ => "Transparent",
            };
            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}