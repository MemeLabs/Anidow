using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using AdonisUI;
using Anidow.Helpers;
using Anidow.Model;
using Serilog;
using Stylet;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsService : PropertyChangedBase
    {
        private readonly ILogger _logger;
        private readonly Assembly _assembly;
        private readonly WindowsStartUp _windowsStartUp;
        private readonly StoreService _storeService;

        public SettingsService(StoreService storeService, ILogger logger,
            Assembly assembly, WindowsStartUp windowsStartUp)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _assembly = assembly;
            _windowsStartUp = windowsStartUp;
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

            var startup = _windowsStartUp.IsEnabled();
            Settings.StartOnWindowsStartUp = startup;
            TempSettings.StartOnWindowsStartUp = startup;

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
            
            switch (Settings.StartOnWindowsStartUp)
            {
                case true when !_windowsStartUp.IsEnabled():
                    await _windowsStartUp.Enable();
                    break;
                case false when _windowsStartUp.IsEnabled():
                    _windowsStartUp.Disable();
                    break;
            }

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