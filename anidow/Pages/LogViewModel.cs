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
    private LogEvent _lastLogEvent;

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
                if (_lastLogEvent.Exception?.Message != logEvent.Exception?.Message)
                {
                    await NotificationUtil.ShowAsync("Error",
                        $"{logEvent.RenderMessage()}\n\n{logEvent.Exception?.Message}",
                        NotificationType.Error);
                }

                break;
        }

        NotifyOfPropertyChange(nameof(ErrorCount));
        NotifyOfPropertyChange(nameof(WarningCount));
        NotifyOfPropertyChange(nameof(InformationCount));
        NotifyOfPropertyChange(nameof(DebugCount));

        _lastLogEvent = logEvent;
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