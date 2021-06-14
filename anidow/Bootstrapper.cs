using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using Anidow.Factories;
using Anidow.Helpers;
using Anidow.Pages;
using Anidow.Properties;
using Anidow.Services;
using Anidow.Torrent_Clients;
using Anidow.Validators;
using FluentScheduler;
using FluentValidation;
using Hardcodet.Wpf.TaskbarNotification;
using Jot;
using Jot.Storage;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Stylet;
using StyletIoC;
#if RELEASE
using AdonisUI.Controls;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

#endif

namespace Anidow
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        private HttpClient _httpClient;
        private ILogger _logger;
        private ShellView _shell;
        private TaskbarIcon _taskBarIcon;
        private UpdateManager _updateManager;

        // Configure the IoC container in here
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            builder.Bind<Assembly>().ToInstance(assembly);
            var tracker = InitTracker();
            builder.Bind<Tracker>().ToInstance(tracker);

            var logViewModel = new LogViewModel();
            InitLogger(logViewModel);

            builder.Bind<ILogger>().ToInstance(_logger);
            builder.Bind<LogViewModel>().ToInstance(logViewModel);

            // Services
            builder.Bind<NyaaService>().ToSelf().InSingletonScope();
            builder.Bind<AnimeBytesService>().ToSelf().InSingletonScope();
            builder.Bind<StoreService>().ToSelf();
            builder.Bind<TorrentService>().ToSelf().InSingletonScope();
            builder.Bind<SettingsService>().ToSelf().InSingletonScope();

            builder.Bind<QBitTorrent>().ToSelf();
            builder.Bind<TorrentClientFactory>().ToSelf().InSingletonScope();

            // Validator IDK how to do validation stuff
            builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentValidationAdapter<>));
            builder.Bind(typeof(IValidator<>)).ToAllImplementations();

            _httpClient = InitHttpClient();
            builder.Bind<HttpClient>().ToInstance(_httpClient);

            _updateManager = new UpdateManager(_httpClient);
            builder.Bind<UpdateManager>().ToInstance(_updateManager);

            _shell = new ShellView(tracker);
            if (Args.Length > 0 && Args[0] == "/autostart")
            {
                _shell.WindowState = WindowState.Minimized;
            }

            _taskBarIcon = _shell.TaskbarIcon;
            builder.Bind<TaskbarIcon>().ToInstance(_shell.TaskbarIcon);
            builder.Bind<ShellView>().ToInstance(_shell);

            //BindViewModels(builder);
        }

        //private void BindViewModels(IStyletIoCBuilder builder)
        //{
        //    builder.Bind<ShellViewModel>().ToSelf();
        //    builder.Bind<AnimeBytesViewModel>().ToSelf().InSingletonScope();
        //    builder.Bind<AnimeBytesRssViewModel>().ToSelf().InSingletonScope();
        //    builder.Bind<AnimeBytesSearchViewModel>().ToSelf().InSingletonScope();
        //    builder.Bind<MainViewModel>().ToSelf().InSingletonScope();
        //    builder.Bind<NyaaViewModel>().ToSelf().InSingletonScope();
        //    builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
        //    builder.Bind<TrackedViewModel>().ToSelf().InSingletonScope();
        //}

        private HttpClient InitHttpClient()
        {
            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = true,
            };
            var httpClient = new HttpClient(clientHandler) {Timeout = TimeSpan.FromSeconds(10)};
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("dnt", "1");
            httpClient.DefaultRequestHeaders.CacheControl = CacheControlHeaderValue.Parse("no-cache");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
            return httpClient;
        }

        private Tracker InitTracker()
        {
            var tracker = new Tracker(new JsonFileStore(Environment.SpecialFolder.CommonApplicationData));
            tracker.Configure<ShellView>()
                   .Id(_ =>
                       $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                   .Properties(w => new {w.Height, w.Width, w.Left, w.Top, w.WindowState})
                   .PersistOn(nameof(ShellView.Closing))
                   .StopTrackingOn(nameof(ShellView.Closing));
            return tracker;
        }

        private void InitLogger(ILogEventSink logViewModel)
        {
            var logLevel = LogEventLevel.Information;
#if DEBUG
            logLevel = LogEventLevel.Verbose;
#endif

            var logConfiguration = new LoggerConfiguration()
                                   .MinimumLevel.Is(logLevel)
                                   .Enrich.FromLogContext()
                                   .WriteTo.Console()
                                   .WriteTo.Sink(logViewModel, logLevel)
                                   .WriteTo.File(
                                       "./logs/log-.txt",
                                       rollingInterval: RollingInterval.Day,
                                       retainedFileCountLimit: 7,
                                       restrictedToMinimumLevel: LogEventLevel.Error);

            _logger = logConfiguration.CreateLogger();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _taskBarIcon?.Dispose();
            JobManager.Stop();
            base.OnExit(e);
        }

        protected override void Configure()
        {
#if RELEASE
            var processes = Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location));
            if (processes.Length > 1)
            {
                MessageBox.Show("Already running!", "Anidow");
                Environment.Exit(0);
            }
#endif

            var secret = Resources.AppCenter_Secret;
            if (!string.IsNullOrWhiteSpace(secret) && !AppCenter.Configured)
            {
                AppCenter.Start(secret, typeof(Crashes), typeof(Analytics));
            }
            else
            {
                _logger.Warning("AppCenter not configured, secret is empty");
            }

            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Language =
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            }

            // Initialize FluentScheduler
            JobManager.Initialize();
            JobManager.JobException += info =>
                _logger.Error(info.Exception, "An error just happened with a scheduled job");

#if SELF_CONTAINED && RELEASE
            var selfContained = true;
#else
            var selfContained = false;
#endif
            _logger.Information(
                $"Anidow v{Assembly.GetExecutingAssembly().GetName().Version} selfcontained: {selfContained}");
        }
#if RELEASE
        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.Fatal(e.Exception, e.Exception.Message);
            if (AppCenter.Configured)
            {
                var messageBox = new MessageBoxModel
                {
                    Text = $"Would you like to report this crash?\n\nCrash message: '{e.Exception.Message}'",
                    Caption = "Anidow crashed!",
                    Icon = MessageBoxImage.Error,
                    Buttons = new[]
                    {
                        MessageBoxButtons.Yes("Yes!"),
                        MessageBoxButtons.No("No!"),
                    },
                    IsSoundEnabled = true,
                };
                var result = MessageBox.Show(messageBox);
                if (result == MessageBoxResult.Yes)
                {
                    Crashes.TrackError(e.Exception);
                }
            }
            else
            {
                MessageBox.Show(e.Exception.Message, "Anidow crashed!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
#endif
    }
}