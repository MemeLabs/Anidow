using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Helpers;
using Anidow.Pages.Components.Settings;
using Anidow.Services;
using Anidow.Utils;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Notifications.Wpf.Core;
using Notifications.Wpf.Core.Controls;
using Serilog;
using Stylet;
#pragma warning disable 1998

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive
    {
        private readonly AboutViewModel _aboutViewModel;
        private readonly AnimeBytesService _animeBytesService;
        private readonly ILogger _logger;
        private readonly LogViewModel _logViewModel;
        private readonly SettingsService _settingsService;
        private readonly IWindowManager _windowManager;
        private readonly UpdateManager _updateManager;
        private List<Screen> _tempViewStorage = new();

        public ShellViewModel(
            MainViewModel mainViewModel,
            NyaaViewModel nyaaViewModel,
            AnimeBytesViewModel animeBytesViewModel,
            SettingsViewModel settingsViewModel,
            TrackedViewModel trackedViewModel,
            SettingsService settingsService,
            AnimeBytesService animeBytesService,
            LogViewModel logViewModel,
            HistoryViewModel historyViewModel,
            AboutViewModel aboutViewModel,
            IWindowManager windowManager,
            UpdateManager updateManager,
            ILogger logger)
        {
            _tempViewStorage.Add(mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel)));
            _tempViewStorage.Add(trackedViewModel ?? throw new ArgumentNullException(nameof(trackedViewModel)));
            _tempViewStorage.Add(animeBytesViewModel ?? throw new ArgumentNullException(nameof(animeBytesViewModel)));
            _tempViewStorage.Add(nyaaViewModel ?? throw new ArgumentNullException(nameof(nyaaViewModel)));
            _tempViewStorage.Add(historyViewModel ?? throw new ArgumentNullException(nameof(historyViewModel)));
            _tempViewStorage.Add(settingsViewModel ?? throw new ArgumentNullException(nameof(settingsService)));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _animeBytesService = animeBytesService ?? throw new ArgumentNullException(nameof(animeBytesService));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string WindowTitle => $"Anidow v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
        public bool CanTestCrash => Debugger.IsAttached;
        public bool Loading { get; private set; } = true;
        public string LoadingMessage { get; private set; }

        protected override void OnInitialActivate()
        {
            LookForUpdates().ContinueWith(async _ =>
            {
                await PrepareDatabase()
                    .ContinueWith(async _ => await LoadSettings());
            });
        }

        private async Task PrepareDatabase()
        {
            LoadingMessage = "Updating Database...";
            await using var db = new TrackContext();
            await db.Database.MigrateAsync();

            var appState = await db.AppStates.SingleOrDefaultAsync();
            if (appState is null)
            {
                await db.AppStates.AddAsync(new AppState
                {
                    Created = DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
            }
        }

        private async Task LoadSettings()
        {
            LoadingMessage = "Loading Settings...";
            // Initalize Settings
            await _settingsService.Initialize();
            foreach (var screen in _tempViewStorage)
            {
                await DispatcherUtil.DispatchAsync(() => Items.Add(screen));
            }

            await Task.Delay(500);
            _tempViewStorage = null;
            Loading = false;
        }

        private async Task LookForUpdates()
        {
#if RELEASE
            LoadingMessage = "Looking for updates...";
            await Task.Delay(300);
            try
            {
                var (check, hasUpdate) = await _updateManager.HasUpdate();
                if (hasUpdate && check != null)
                {
                    LoadingMessage = "Found update! updating now...";
                    await Task.Delay(300);
                    await _updateManager.Update(check, () => RequestClose());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating anidow");
            }
#endif
        }

        public async Task TestCrash()
        {
            await NotificationUtil.ShowAsync("Test Crash", "Crashing Anidow in 3 seconds", NotificationType.Warning);
            await Task.Delay(3.Seconds());
            throw new Exception("Test crash");
        }

        public void ShowLogs()
        {
            _windowManager.ShowWindow(_logViewModel, this);
        }

        public void Close()
        {
            RequestClose();
        }

        public void OpenGithub()
        {
            LinkUtil.Open("https://github.com/MemeLabs/Anidow");
        }

        public void OpenAbout()
        {
            _windowManager.ShowDialog(_aboutViewModel);
        }

        public void ShowWindow()
        {
            if (AdonisWindow == null)
            {
                return;
            }

            AdonisWindow.Show();
            AdonisWindow.WindowState = WindowState.Normal;
        }

        private AdonisWindow AdonisWindow { get; set; }

        public void Window_Loaded(object sender, RoutedEventArgs _)
        {
            AdonisWindow ??= (AdonisWindow)sender;
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            AdonisWindow ??= (AdonisWindow)sender;
            if (_settingsService.Settings.MinimizeToNotificationArea)
            {
                AdonisWindow.Hide();
                e.Cancel = true;
            }
        }
    }
}