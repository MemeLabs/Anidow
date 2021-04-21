using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Stylet;

namespace Anidow.Pages
{
    public class LogViewModel: PropertyChangedBase, ILogEventSink
    {
        public ObservableCollection<LogEvent> Items { get; }
        public LogViewModel()
        {
            Items = new ObservableCollection<LogEvent>();
        }

        public void Emit(LogEvent logEvent)
        {
            Execute.OnUIThreadSync(() => Items.Insert(0, logEvent));
        }
    }
}
