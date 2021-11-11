using System;
using System.IO;
using System.Linq;
using Anidow.Utils;
using Notifications.Wpf.Core;
using Serilog.Core;
using Serilog.Events;
using Stylet;

namespace Anidow.Pages;

public class LogViewModel : Screen, ILogEventSink
{
    private string _lastLogEvent;

    public LogViewModel()
    {
        Items = new BindableCollection<LogEvent>();
    }

    public IObservableCollection<LogEvent> Items { get; }
    public int ErrorCount => Items.Count(i => i.Level == LogEventLevel.Error);
    public int InformationCount => Items.Count(i => i.Level == LogEventLevel.Information);
    public int WarningCount => Items.Count(i => i.Level == LogEventLevel.Warning);
    public int DebugCount => Items.Count(i => i.Level == LogEventLevel.Debug);

    public bool ShowError { get; set; } = true;
    public bool ShowInformation { get; set; } = true;
    public bool ShowWarning { get; set; } = true;
    public bool ShowDebug { get; set; }

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
                
                var msg = logEvent.RenderMessage();

                if (msg != _lastLogEvent)
                {
                    await NotificationUtil.ShowAsync("Error", msg, NotificationType.Error);
                    _lastLogEvent = msg;
                }

                break;
        }

        //NotifyOfPropertyChange(nameof(ErrorCount));
        //NotifyOfPropertyChange(nameof(WarningCount));
        //NotifyOfPropertyChange(nameof(InformationCount));
        //NotifyOfPropertyChange(nameof(DebugCount));
        Refresh();
    }

    public void OpenLogsFolder()
    {
        if (!Directory.Exists("logs"))
        {
            Directory.CreateDirectory("logs");
        }

        ProcessUtil.OpenFolder("logs");
    }
}