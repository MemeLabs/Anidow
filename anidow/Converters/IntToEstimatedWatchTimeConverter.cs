using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;

namespace Anidow.Converters;

internal class IntToEstimatedWatchTimeConverter : IValueConverter
{
    public static readonly IntToEstimatedWatchTimeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = (int?)value;
        var episodeLength = TimeSpan.FromMinutes(23) * s;
        return episodeLength?.Humanize(2)
                            .Replace(" hours", "h").Replace(" hour", "h")
                            .Replace(" minutes", "m").Replace(" minute", "m");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value;
}