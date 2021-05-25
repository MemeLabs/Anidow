using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Anidow.Services;
using Anidow.Utils;
using Microsoft.AppCenter.Crashes;
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
        private readonly AboutViewModel _aboutViewModel;
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
            AboutViewModel aboutViewModel,
            IWindowManager windowManager,
            ILogger logger)
        {
            Items.Add(mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel)));
            Items.Add(trackedViewModel ?? throw new ArgumentNullException(nameof(trackedViewModel)));
            Items.Add(animeBytesViewModel ?? throw new ArgumentNullException(nameof(animeBytesViewModel)));
            Items.Add(nyaaViewModel ?? throw new ArgumentNullException(nameof(nyaaViewModel)));
            Items.Add(historyViewModel ?? throw new ArgumentNullException(nameof(historyViewModel)));
            Items.Add(settingsViewModel ?? throw new ArgumentNullException(nameof(settingsService)));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _animeBytesService = animeBytesService ?? throw new ArgumentNullException(nameof(animeBytesService));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ActiveItem = mainViewModel;
        }

        public string WindowTitle => $"Anidow v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";

        public bool CanForceCheck { get; set; } = true;
        public string NextCheckIn { get; set; }
        public Timer NextCheckTimer { get; set; }
        public bool CanTestCrash => Debugger.IsAttached;

        protected override async void OnInitialActivate()
        {
            await _settingsService.Init();
            _animeBytesService.InitTracker();
            NextCheckTimer = new Timer(1000);
            NextCheckTimer.Elapsed += NextCheckTimerOnElapsed;
            NextCheckTimer.Start();
        }

        public void TestCrash()
        {
            Crashes.GenerateTestCrash();
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

        public void OpenAbout()
        {
            _windowManager.ShowDialog(_aboutViewModel);
        }
    }
}