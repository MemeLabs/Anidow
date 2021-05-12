using System.Collections.ObjectModel;
using Serilog.Core;
using Serilog.Events;
using Stylet;

namespace Anidow.Pages
{
    public class LogViewModel : Screen, ILogEventSink
    {
        public LogViewModel()
        {
            Items = new ObservableCollection<LogEvent>();
        }

        public ObservableCollection<LogEvent> Items { get; }

        public void Emit(LogEvent logEvent)
        {
            Execute.OnUIThreadSync(() => Items.Insert(0, logEvent));
        }
    }
}