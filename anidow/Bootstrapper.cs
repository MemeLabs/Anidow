﻿using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Markup;
using Anidow.Database;
using Anidow.Factories;
using Anidow.Pages;
using Anidow.Services;
using Anidow.Torrent_Clients;
using Anidow.Validators;
using FluentValidation;
using Hardcodet.Wpf.TaskbarNotification;
using Jot;
using Jot.Storage;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Stylet;
using StyletIoC;

#if RELEASE
using System.Reflection;
using System.Windows.Threading;
using Onova;
using Onova.Models;
using Onova.Services;

using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
#endif

namespace Anidow
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        private HttpClient _httpClient;
        private ILogger _logger;
        private TaskbarIcon _taskBarIcon;

        // Configure the IoC container in here
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
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
            var shell = new ShellView(tracker);
            _taskBarIcon = shell.TaskbarIcon;
            builder.Bind<TaskbarIcon>().ToInstance(shell.TaskbarIcon);
            builder.Bind<ShellView>().ToInstance(shell);

            //BindViewModels(builder);
        }

        //private void BindViewModels(IStyletIoCBuilder builder)
        //{
        //    builder.Bind<ShellViewModel>().ToSelf();
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
                UseCookies = true
            };
            var httpClient = new HttpClient(clientHandler) { Timeout = TimeSpan.FromSeconds(10) };
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
                .Id(_ => $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                .PersistOn(nameof(ShellView.Closing))
                .StopTrackingOn(nameof(ShellView.Closing));
            return tracker;
        }

        private void InitLogger(ILogEventSink logViewModel)
        {
            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Sink(logViewModel, LogEventLevel.Verbose)
                .WriteTo.File(
                    "./logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);

            _logger = logConfiguration.CreateLogger();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _taskBarIcon?.Dispose();
        }

        protected override async void Configure()
        {
            // Perform any other configuration before the application starts
            {
                await using var db = new TrackContext();
                await db.Database.MigrateAsync();
            }
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Language =
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            }

            var selfContained = false;
#if SELF_CONTAINED && RELEASE
            selfContained = true;
#endif

#if RELEASE
            try
            {
                var manager = new UpdateManager(
                    AssemblyMetadata.FromAssembly(
                        Assembly.GetEntryAssembly(),
                        System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName),
                    new GithubPackageResolver(
                        _httpClient,
                        "MemeLabs",
                        "Anidow",
                        selfContained ? "anidow-full.zip" : "anidow.zip"),
                    new ZipPackageExtractor());

                var check = await manager.CheckForUpdatesAsync();
                if (!check.CanUpdate || check.LastVersion == null)
                {
                    return;
                }

                // Prepare the latest update
                await manager.PrepareUpdateAsync(check.LastVersion);

                // Launch updater and exit
                manager.LaunchUpdater(check.LastVersion);

                Environment.Exit(0);
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating the app");
            }
#endif
        }
#if RELEASE
        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.Fatal(e.Exception, e.Exception.Message);
            MessageBox.Show($"{e.Exception.Message}", string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
        }
#endif
    }
}