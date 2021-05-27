using System;
using Anidow.Utils;
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

        public void Emit(LogEvent logEvent)
        {
            try
            {
                DispatcherUtil.DispatchSync(() => Items.Insert(0, logEvent));
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}