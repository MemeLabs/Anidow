using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using AdonisUI.Controls;
using Anidow.Services;
using Anidow.Utils;
using Humanizer;
using Notifications.Wpf.Core;
using Stylet;

#pragma warning disable 1998

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ShellViewModel : Conductor<Screen>.Collection.OneActive
    {
        public static ShellViewModel Instance;
        private readonly AboutViewModel _aboutViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly SettingsService _settingsService;
        private readonly IWindowManager _windowManager;
        public MainViewModel MainViewModel;

        public ShellViewModel(
            MainViewModel mainViewModel,
            StartupViewModel startupViewModel,
            SettingsViewModel settingsViewModel,
            SettingsService settingsService,
            LogViewModel logViewModel,
            AboutViewModel aboutViewModel,
            IWindowManager windowManager)
        {
            SettingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

            Instance ??= this;
            startupViewModel.OnFinished = () =>
            {
                Items.Add(mainViewModel);
                ChangeActiveItem(mainViewModel, true);
                CanToggleSettings = true;
            };
            Items.Add(startupViewModel);
            Items.Add(settingsViewModel);
            ChangeActiveItem(startupViewModel, false);
        }

        public SettingsViewModel SettingsViewModel { get; }

        public bool CanToggleSettings { get; set; }


        public string WindowTitle => $"Anidow v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
        public bool CanTestCrash => Debugger.IsAttached;

        private AdonisWindow AdonisWindow { get; set; }

        public void ToggleSettings()
        {
            ActiveItem = ActiveItem == MainViewModel ? SettingsViewModel : MainViewModel;
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

        public void Window_Loaded(object sender, RoutedEventArgs _)
        {
            AdonisWindow ??= (AdonisWindow) sender;
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            AdonisWindow ??= (AdonisWindow) sender;
            if (_settingsService.Settings is {MinimizeToNotificationArea: true})
            {
                AdonisWindow.Hide();
                e.Cancel = true;
            }
        }
    }
}