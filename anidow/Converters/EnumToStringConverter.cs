using System;
using System.Globalization;
using System.Windows.Data;
using Anidow.Enums;

namespace Anidow.Converters;

public class EnumToStringConverter : IValueConverter
{
    public static readonly EnumToStringConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = ((AnimeStatus)value!).ToString();
        return s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (AnimeStatus)value!;
}