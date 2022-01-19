using System;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Threading.Tasks;
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

        if (!string.IsNullOrWhiteSpace(Settings.AniListJwt))
        {
            try
            {
                Settings.AniListJwtExpires = GetExpiresFromJwt();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed parsing jwt");
            }
        }

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

    private DateTime GetExpiresFromJwt()
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(Settings.AniListJwt);

        // Unix timestamp is seconds past epoch
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(jwtSecurityToken.Payload.Exp ?? 0).ToLocalTime();
        return dateTime;
    }

    public async Task Save()
    {
        CanSave = false;

        await _storeService.Save(TempSettings, "settings.json");
        Settings = await _storeService.Load<SettingsModel>("settings.json");

        if (!string.IsNullOrWhiteSpace(Settings.AniListJwt))
        {
            try
            {
                Settings.AniListJwtExpires = GetExpiresFromJwt();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed parsing jwt");
            }
        }

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