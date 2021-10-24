using System;
using System.Globalization;
using System.Threading;
using System.Windows.Data;
using Serilog.Events;

namespace Anidow.Converters;

public class LogEventRenderConverter : IValueConverter
{
    public static readonly LogEventRenderConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEvent @event)
        {
            return @event.RenderMessage(Thread.CurrentThread.CurrentUICulture);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}