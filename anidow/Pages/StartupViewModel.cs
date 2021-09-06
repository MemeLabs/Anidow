using System;
using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Helpers;
using Anidow.Pages.Components.Settings;
using Anidow.Services;
using Anidow.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    public class StartupViewModel : Screen
    {
        private readonly AppService _appService;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly SettingsSetupWizardViewModel _setupWizardViewModel;
        private readonly UpdateManager _updateManager;
        private readonly IWindowManager _windowManager;

        public StartupViewModel(
            UpdateManager updateManager,
            SettingsService settingsService,
            ILogger logger,
            IWindowManager windowManager,
            AppService appService,
            SettingsSetupWizardViewModel setupWizardViewModel)
        {
            _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
            _setupWizardViewModel =
                setupWizardViewModel ?? throw new ArgumentNullException(nameof(setupWizardViewModel));
        }

        public string LoadingMessage { get; private set; }
        public Action OnFinished { get; set; }

        public bool ShowError { get; set; }
        public string ErrorStack { get; set; }

        // protected override void OnInitialActivate()
        // {
        //     LookForUpdates().ContinueWith(async _ =>
        //     {
        //         await PrepareDatabase()
        //             .ContinueWith(async _ => await LoadSettings()
        //                 .ContinueWith(async _ => await ShowSetupWizard()));
        //     });
        // }

        protected override async void OnViewLoaded()
        {
            await LookForUpdates();
            // causes ui freeze when not in Task.Run
            await Task.Run(async () => await PrepareDatabase());
            await LoadSettings();
            await ShowSetupWizard();
        }

        private async Task ShowSetupWizard()
        {
            try
            {
                await using var db = new TrackContext();
                var appState = await db.AppStates.FirstOrDefaultAsync();
#if DEBUG
                if (appState is { FirstStart: false })
#else
                if (appState is {FirstStart: true})
#endif
                {
                    appState.FirstStart = false;
                    await db.SaveChangesAsync();

                    DispatcherUtil.DispatchSync(() => _windowManager.ShowDialog(_setupWizardViewModel));
                }

                // Finished everything! time to show homeview
                OnFinished?.Invoke();
            }
            catch (Exception e)
            {
                var msg = JsonConvert.SerializeObject(e);
                LoadingMessage = "Error";
                ShowError = !string.IsNullOrEmpty(msg);
                ErrorStack = msg;
                throw;
            }
        }

        private async Task PrepareDatabase()
        {
            try
            {
                LoadingMessage = "Updating Database...";
                await using (var db = new TrackContext())
                {
                    await db.Database.MigrateAsync();
                }

                await _appService.Initialize();
            }
            catch (Exception e)
            {
                var msg = JsonConvert.SerializeObject(e);
                LoadingMessage = "Error";
                ShowError = !string.IsNullOrEmpty(msg);
                ErrorStack = msg;
                throw;
            }
        }

        private async Task LoadSettings()
        {
            try
            {
                LoadingMessage = "Loading Settings...";
                // Initalize Settings
                await _settingsService.Initialize();
                await Task.Delay(500);
            }
            catch (Exception e)
            {
                var msg = JsonConvert.SerializeObject(e);
                LoadingMessage = "Error";
                ShowError = !string.IsNullOrEmpty(msg);
                ErrorStack = msg;
                throw;
            }
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
                    await _updateManager.Update(check, () => Application.Current.Shutdown());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating anidow");
            }
#endif
        }
    }
}