using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AdonisUI;
using Anidow.Model;
using Serilog;
using Stylet;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsService : PropertyChangedBase
    {
        private readonly ILogger _logger;
        private readonly StoreService _storeService;

        public SettingsService(StoreService storeService, ILogger logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        public bool CanSave { get; set; }

        public SettingsModel Settings { get; private set; }

        /// <summary>
        ///     this is used for the SettingsView as a temporary setting
        ///     after the user saves it it will become the main setting everything uses
        /// </summary>
        public SettingsModel TempSettings { get; private set; }

        public event EventHandler SettingsSavedEvent; // event

        public async Task Init()
        {
            if (TempSettings != null)
            {
                _logger.Warning("settings already initialized");
                return;
            }

            TempSettings = await _storeService.Load<SettingsModel>("settings.json") ?? new SettingsModel();
            Settings = await _storeService.Load<SettingsModel>("settings.json") ?? new SettingsModel();

            TempSettings.PropertyChanged += SettingsOnPropertyChanged;
            TempSettings.QBitTorrent.PropertyChanged += SettingsOnPropertyChanged;
            TempSettings.NyaaSettings.PropertyChanged += SettingsOnPropertyChanged;
            TempSettings.AnimeBytesSettings.PropertyChanged += SettingsOnPropertyChanged;

            ResourceLocator.SetColorScheme(Application.Current.Resources,
                TempSettings.IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
        }

        public async Task Save()
        {
            await _storeService.Save(TempSettings, "settings.json");
            Settings = await _storeService.Load<SettingsModel>("settings.json");

            _logger.Information("saved setting's");

            ResourceLocator.SetColorScheme(Application.Current.Resources,
                TempSettings.IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);

            CanSave = false;
            SettingsSavedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CanSave = true;
        }
    }
}