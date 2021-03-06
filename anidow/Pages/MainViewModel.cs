// // Created: 14-06-2021 11:36

using System;
using System.Linq;
using System.Threading.Tasks;
using Anidow.Pages.Components.Status;
using Anidow.Services;
using Stylet;

namespace Anidow.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public class MainViewModel : Conductor<Screen>.Collection.OneActive
{
    public static MainViewModel Instance;
    private readonly AboutViewModel _aboutViewModel;
    private readonly NotifyViewModel _notifyViewModel;
    private readonly SettingsService _settingsService;
    private readonly IWindowManager _windowManager;
    private int _initialRefreshTime;

    public MainViewModel(
        HomeViewModel homeViewModel,
        NyaaViewModel nyaaViewModel,
        AnimeBytesViewModel animeBytesViewModel,
        TrackedViewModel trackedViewModel,
        HistoryViewModel historyViewModel,
        AboutViewModel aboutViewModel,
        NotifyViewModel notifyViewModel,
        StatusViewModel statusViewModel,
        SettingsService settingsService,
        IWindowManager windowManager)
    {
        Instance = this;
        StatusViewModel = statusViewModel ?? throw new ArgumentNullException(nameof(statusViewModel));
        Items.Add(homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel)));
        Items.Add(trackedViewModel ?? throw new ArgumentNullException(nameof(trackedViewModel)));
        Items.Add(animeBytesViewModel ?? throw new ArgumentNullException(nameof(animeBytesViewModel)));
        Items.Add(nyaaViewModel ?? throw new ArgumentNullException(nameof(nyaaViewModel)));
        Items.Add(notifyViewModel ?? throw new ArgumentNullException(nameof(notifyViewModel)));
        Items.Add(historyViewModel ?? throw new ArgumentNullException(nameof(historyViewModel)));
        _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _notifyViewModel = notifyViewModel;
    }

    public StatusViewModel StatusViewModel { get; }

    public void OpenAbout()
    {
        _windowManager.ShowDialog(_aboutViewModel);
    }

    protected override void OnInitialActivate()
    {
        ChangeActiveItem(Items.FirstOrDefault(), false);
        StatusViewModel.Init();
        Task.Run(async () =>
        {
            await _notifyViewModel.Init();
            if (_notifyViewModel.Items.Count > 0)
            {
#if RELEASE
                StatusViewModel.StartNyaa();
                StatusViewModel.StartAnimeBytesAll();
#endif
            }
        });

        _settingsService.SettingsSavedEvent += OnSettingsSavedEvent;
        if (_settingsService.Settings.StartTrackerAnimeBytesOnLaunch)
        {
#if RELEASE
                StatusViewModel.StartAnimeBytesAiring();
#endif
        }
    }


    private void OnSettingsSavedEvent(object sender, EventArgs e)
    {
        if (_settingsService.Settings.RefreshTime == _initialRefreshTime)
        {
            return;
        }

        StatusViewModel.StartAnimeBytesAiring();

        _initialRefreshTime = _settingsService.Settings.RefreshTime;

        if (string.IsNullOrWhiteSpace(_settingsService.Settings.AnimeBytesSettings.PassKey))
        {
            return;
        }

        StatusViewModel.StartAnimeBytesAiring();
    }
}