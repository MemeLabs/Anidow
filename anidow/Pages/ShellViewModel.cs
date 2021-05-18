using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Anidow.Services;
using Anidow.Utils;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive
    {
        private readonly AnimeBytesService _animeBytesService;
        private readonly ILogger _logger;
        private readonly LogViewModel _logViewModel;
        private readonly SettingsService _settingsService;
        private readonly IWindowManager _windowManager;

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
            IWindowManager windowManager,
            ILogger logger)
        {
            Items.Add(mainViewModel);
            Items.Add(trackedViewModel);
            Items.Add(animeBytesViewModel);
            Items.Add(nyaaViewModel);
            Items.Add(historyViewModel);
            Items.Add(settingsViewModel);
            _settingsService = settingsService;
            _animeBytesService = animeBytesService;
            _logViewModel = logViewModel;
            _windowManager = windowManager;
            _logger = logger;
            ActiveItem = mainViewModel;
        }

        public string WindowTitle => $"Anidow v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";

        public bool CanForceCheck { get; set; } = true;
        public string NextCheckIn { get; set; }
        public Timer NextCheckTimer { get; set; }

        protected override async void OnInitialActivate()
        {
            await _settingsService.Init();
            _animeBytesService.InitTracker();
            NextCheckTimer = new Timer(1000);
            NextCheckTimer.Elapsed += NextCheckTimerOnElapsed;
            NextCheckTimer.Start();
        }

        private void NextCheckTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var lastCheck = _animeBytesService.LastCheck;
            var nextCheck = lastCheck + TimeSpan.FromMinutes(_settingsService.Settings.RefreshTime);
            NextCheckIn = $"next check in {nextCheck - DateTime.Now:mm\\:ss} min";
        }

        public async Task ForceCheck()
        {
            CanForceCheck = false;
            await _animeBytesService.CheckForNewEpisodes();
            CanForceCheck = true;
        }

        public void ShowLogs()
        {
            _windowManager.ShowWindow(_logViewModel, this);
        }

        public void Close()
        {
            Environment.Exit(0);
        }

        public void OpenGithub()
        {
            LinkUtil.Open("https://github.com/MemeLabs/Anidow");
        }
    }
}