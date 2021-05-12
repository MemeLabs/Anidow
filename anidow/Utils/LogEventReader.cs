using System;
using System.Globalization;
using System.Threading;
using System.Windows.Data;
using Serilog.Events;

namespace Anidow.Utils
{
    public class LogEventRender : IValueConverter
    {
        public static readonly LogEventRender Instance = new();

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
}