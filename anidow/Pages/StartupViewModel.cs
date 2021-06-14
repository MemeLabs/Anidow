using System;
using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Helpers;
using Anidow.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    public class StartupViewModel : Screen
    {
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly UpdateManager _updateManager;

        public StartupViewModel(UpdateManager updateManager, SettingsService settingsService, ILogger logger)
        {
            _updateManager = updateManager;
            _settingsService = settingsService;
            _logger = logger;
        }

        public string LoadingMessage { get; private set; }
        public Action OnFinished { get; set; }


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
            await Task.Delay(500);

            OnFinished?.Invoke();
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