using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using AdonisUI;
using Anidow.Model;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Notifications.Wpf.Core;
using Serilog;
using Stylet;

namespace Anidow.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class SettingsService : PropertyChangedBase
{
    private readonly Assembly _assembly;
    private readonly ILogger _logger;
    private readonly StoreService _storeService;
    private readonly TaskbarIcon _taskbarIcon;

    public SettingsService(StoreService storeService, ILogger logger,
        TaskbarIcon taskbarIcon, Assembly assembly)
    {
        _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
        _assembly = assembly;
    }

    public bool CanSave { get; set; }

    public SettingsModel Settings { get; private set; }

    /// <summary>
    ///     this is used for the SettingsView as a temporary setting
    ///     after the user saves it it will become the main setting everything uses
    /// </summary>
    public SettingsModel TempSettings { get; private set; }

    public event EventHandler SettingsSavedEvent; // event

    public async Task Initialize()
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

        //ResourceLocator.SetColorScheme(Application.Current.Resources,
        //    TempSettings.IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);

        if (Settings.FirstStart)
        {
            CanSave = true;
        }
    }

    public async Task Save()
    {
        CanSave = false;

        await _storeService.Save(TempSettings, "settings.json");
        Settings = await _storeService.Load<SettingsModel>("settings.json");

        _logger.Information("saved setting's");

        ////ResourceLocator.SetColorScheme(Application.Current.Resources,
        ////    TempSettings.IsDark ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);

        SettingsSavedEvent?.Invoke(this, EventArgs.Empty);
        await NotificationUtil.ShowAsync("Settings", "Saved!", NotificationType.Success);
    }

    private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        CanSave = true;
    }
}