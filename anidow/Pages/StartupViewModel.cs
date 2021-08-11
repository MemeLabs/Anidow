using System;
using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Helpers;
using Anidow.Pages.Components.Settings;
using Anidow.Services;
using Anidow.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    public class StartupViewModel : Screen
    {
        private readonly ILogger _logger;
        private readonly IWindowManager _windowManager;
        private readonly SettingsSetupWizardViewModel _setupWizardViewModel;
        private readonly SettingsService _settingsService;
        private readonly UpdateManager _updateManager;

        public StartupViewModel(UpdateManager updateManager, SettingsService settingsService, ILogger logger,
            IWindowManager windowManager, SettingsSetupWizardViewModel setupWizardViewModel)
        {
            _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _setupWizardViewModel = setupWizardViewModel ?? throw new ArgumentNullException(nameof(setupWizardViewModel));
        }

        public string LoadingMessage { get; private set; }
        public Action OnFinished { get; set; }


        protected override void OnInitialActivate()
        {
            LookForUpdates().ContinueWith(async _ =>
            {
                await PrepareDatabase()
                    .ContinueWith(async _ => await LoadSettings()
                        .ContinueWith(async _ => await ShowSetupWizard()));
            });
            
        }
        
        private async Task ShowSetupWizard()
        {
            await using var db = new TrackContext();
            var appState = await db.AppStates.SingleOrDefaultAsync();
#if DEBUG
            if (appState is {FirstStart: false})
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
            await Task.Delay(500);
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
    }
}