using System;
using Anidow.Utils;
using Notifications.Wpf.Core;
using Serilog.Core;
using Serilog.Events;
using Stylet;

namespace Anidow.Pages
{
    public class LogViewModel : Screen, ILogEventSink
    {
        public LogViewModel()
        {
            Items = new BindableCollection<LogEvent>();
        }

        public IObservableCollection<LogEvent> Items { get; }

        private LogEvent _lastLogEvent;
        public async void Emit(LogEvent logEvent)
        {
            try
            {
                DispatcherUtil.DispatchSync(() => Items.Add(logEvent));
            }
            catch (Exception)
            {
                //ignore
            }

            switch (logEvent.Level)
            {
                case LogEventLevel.Error:
                    if (_lastLogEvent.Exception?.Message != logEvent.Exception?.Message)
                    {
                        await NotificationUtil.ShowAsync("Error",
                            $"{logEvent.RenderMessage()}\n\n{logEvent.Exception?.Message}",
                            NotificationType.Error);
                    }
                    break;
            }

            _lastLogEvent = logEvent;
        }
    }
}