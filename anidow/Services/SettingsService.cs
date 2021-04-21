using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AdonisUI;
using Anidow.Model;
using Anidow.Validators;
using Serilog;
using Stylet;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsService : PropertyChangedBase
    {
        private readonly ILogger _logger;
        private readonly SettingsValidation _settingsValidation;
        private readonly StoreService _storeService;
        private SettingsModel _settings;
        private SettingsModel _settingsOriginal;
        public event EventHandler SettingsSaved; // event

        public SettingsService(StoreService storeService, SettingsValidation settingsValidation, ILogger logger)
        {
            _storeService = storeService;
            _settingsValidation = settingsValidation;
            _logger = logger;
        }

        public bool CanSave { get; set; }

        public async Task Init()
        {
            if (_settings != null)
            {
                return;
            }

            _settings = await _storeService.Load<SettingsModel>("settings.json") ?? new SettingsModel();
            _settingsOriginal = await _storeService.Load<SettingsModel>("settings.json") ?? new SettingsModel();

            _settings.PropertyChanged += SettingsOnPropertyChanged;
            _settings.QBitTorrent.PropertyChanged += SettingsOnPropertyChanged;
            _settings.NyaaSettings.PropertyChanged += SettingsOnPropertyChanged;
            _settings.AnimeBytesSettings.PropertyChanged += SettingsOnPropertyChanged;

            ResourceLocator.SetColorScheme(Application.Current.Resources,
                _settings.IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
        }

        public SettingsModel GetSettings() => _settingsOriginal;

        public SettingsModel GetSettingsForSettingsView() => _settings;

        public async Task Save()
        {
            await _storeService.Save(_settings, "settings.json");
            _settingsOriginal = await _storeService.Load<SettingsModel>("settings.json");

            _logger.Information("saved setting's");

            ResourceLocator.SetColorScheme(Application.Current.Resources,
                _settings.IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);

            CanSave = false;
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CanSave = true;
        }
    }
}