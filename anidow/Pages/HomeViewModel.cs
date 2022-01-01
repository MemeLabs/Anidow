using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Interfaces;
using Anidow.Model;
using Anidow.Pages.Components.AnimeInfo;
using Anidow.Pages.Components.Settings;
using Anidow.Pages.Components.Status;
using Anidow.Pages.Components.Tracked;
using Anidow.Services;
using Anidow.Utils;
using BencodeNET.Torrents;
using FluentScheduler;
using Hardcodet.Wpf.TaskbarNotification;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace Anidow.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public class HomeViewModel : Conductor<IEpisode>.Collection.OneActive, IHandle<DownloadEvent>,
    IHandle<RefreshHomeEvent>, IHandle<AddToHomeEvent>
{
    private readonly IEventAggregator _eventAggregator;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly NotifyService _notifyService;
    private readonly SettingsService _settingsService;
    private readonly SettingsSetupWizardViewModel _settingsSetupWizardViewModel;
    private readonly TaskbarIcon _taskbarIcon;
    private readonly TorrentService _torrentService;
    private readonly TrackedAnimeEditContentViewModel _trackedAnimeEditContentViewModel;
    private readonly IWindowManager _windowManager;
    private bool _updatingTorrents = false;
    private bool _listViewLoaded = false;


    public HomeViewModel(IEventAggregator eventAggregator, ILogger logger,
        IWindowManager windowManager, AnimeBytesService animeBytesService,
        TorrentService torrentService, SettingsService settingsService,
        SettingsSetupWizardViewModel settingsSetupWizardViewModel,
        TaskbarIcon taskbarIcon, HttpClient httpClient,
        StatusViewModel statusViewModel,
        TrackedAnimeEditContentViewModel trackedAnimeEditContentViewModel,
        NotifyService notifyService)
    {
        StatusViewModel = statusViewModel ?? throw new ArgumentNullException(nameof(statusViewModel));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        AnimeBytesService = animeBytesService ?? throw new ArgumentNullException(nameof(animeBytesService));
        _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _trackedAnimeEditContentViewModel = trackedAnimeEditContentViewModel ??
                                            throw new ArgumentNullException(nameof(trackedAnimeEditContentViewModel));
        _notifyService = notifyService ?? throw new ArgumentNullException(nameof(notifyService));
        _settingsSetupWizardViewModel = settingsSetupWizardViewModel ??
                                        throw new ArgumentNullException(nameof(settingsSetupWizardViewModel));
        DisplayName = "Home";
    }

    public string BadgeContent { get; set; }
    public AnimeBytesService AnimeBytesService { get; set; }
    public StatusViewModel StatusViewModel { get; }
    public Timer NextCheckTimer { get; set; }
    public IObservableCollection<FutureEpisode> AnimesToday { get; set; } = new BindableCollection<FutureEpisode>();
    public bool IsEnabled => _settingsService != null && !_settingsService.Settings.FirstStart;


    public async void Handle(AddToHomeEvent message)
    {
        Items.Add(message.Episode);
        await GetAiringEpisodesForToday();
    }

    public async void Handle(DownloadEvent message)
    {
        var torrent = message.Torrent;
        var item = message.Item switch
        {
            NyaaTorrentItem nyaa => new Episode
            {
                Name = nyaa.Name,
                Site = Site.Nyaa,
                Released = nyaa.Released,
                File = torrent?.FileMode == TorrentFileMode.Single
                    ? Path.Join(nyaa.Folder, torrent.File.FileName)
                    : null,
                Folder = nyaa.Folder,
                Link = nyaa.Link,
                DownloadLink = nyaa.DownloadLink,
                TorrentId = torrent?.GetInfoHash(),
            },
            AnimeBytesTorrentItem ab => new Episode
            {
                Name = ab.Name,
                Site = Site.AnimeBytes,
                Released = ab.Released,
                Folder = ab.Folder,
                Link = ab.GroupUrl,
                DownloadLink = ab.DownloadLink,
                Cover = ab.Cover,
                File = torrent?.FileMode == TorrentFileMode.Single
                    ? Path.Join(ab.Folder, torrent.File.FileName)
                    : null,
                TorrentId = torrent?.GetInfoHash(),
            },
            AnimeBytesScrapeAnime ab => new Episode
            {
                Name = CreatePropertyEpisode(ab),
                Site = Site.AnimeBytes,
                Released = DateTime.Parse(ab.SelectedTorrent.UploadTime),
                Folder = ab.Folder,
                Link = $"https://animebytes.tv/torrent/{ab.SelectedTorrent.ID}/group",
                DownloadLink = ab.SelectedTorrent.DownloadLink,
                Cover = ab.Image,
                File = torrent?.FileMode == TorrentFileMode.Single
                    ? Path.Join(ab.SelectedTorrent.Folder, torrent.File.FileName)
                    : null,
                TorrentId = torrent?.GetInfoHash(),
            },
            _ => throw new NotSupportedException(nameof(message.Item)),
        };

        await using var db = new TrackContext();

        async Task GetAndSetAnime(TrackContext ctx, string id)
        {
            var anime = await ctx.Anime
                                .Include(a => a.AniListAnime)
                                .Include(a => a.CoverData)
                                .FirstOrDefaultAsync(a => a.GroupId == id);
            if (anime is not null)
            {
                item.Anime ??= anime;
                item.AnimeId = anime.GroupId;
                item.CoverData ??= anime.CoverData;
            }
        }

        switch (message.Item)
        {
            case AnimeBytesTorrentItem abti:
                {
                    await GetAndSetAnime(db, abti.GroupId);
                    break;
                }
            case AnimeBytesScrapeAnime absa:
                {
                    await GetAndSetAnime(db, absa.ID.ToString());
                    break;
                }
        }

        if (_settingsService.Settings.Notifications)
        {
            _taskbarIcon.ShowBalloonTip("Added", item.Name, BalloonIcon.None);
        }
        else
        {
            await NotificationUtil.ShowAsync("Added", item.Name);
        }

        await db.AddAsync(item);
        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(item.AnimeId) && item.Anime is null)
        {
            var animeDb = await db.Anime
                                  .Include(a => a.AniListAnime)
                                  .Include(a => a.CoverData)
                                  .FirstOrDefaultAsync(a => a.GroupId == item.AnimeId);
            item.Anime = animeDb;
        }

        Items.Add(item);
        await GetAiringEpisodesForToday();
    }

    public void Handle(RefreshHomeEvent _)
    {
        Debouncer.DebounceAction("home_refresh", async _ =>
        {
            await LoadEpisodes();
            await GetAiringEpisodesForToday();
        });
    }

    private string CreatePropertyEpisode(AnimeBytesScrapeAnime ab)
    {
        try
        {
            var parts = ab.SelectedTorrent.Property.Split('|').ToList();
            parts.Insert(parts.Count - 1, $" {ab.SelectedTorrent.EditionData.EditionTitle} ");
            return $"{ab.FullName} :: {string.Join('|', parts)}";
        }
        catch (Exception)
        {
            return $"{ab.FullName} :: {ab.SelectedTorrent.Property}";
        }
    }

    public async Task HideItem(IEpisode episode)
    {
        episode ??= ActiveItem;
        var index = Items.IndexOf(episode);
        if (index == -1)
        {
            return;
        }

        episode.Hide = true;
        episode.HideDate = DateTime.UtcNow;

        try
        {
            await episode.UpdateInDatabase();
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed updating episode in database");
        }

        Items.Remove(episode);
        DeselectItem();

        await NotificationUtil.ShowUndoAsync(episode.Name, "Moved to History!", async () =>
        {
            episode.Hide = false;
            episode.HideDate = default;
            await episode.UpdateInDatabase();
            Items.Add(episode);
        }, null, TimeSpan.FromSeconds(5));
    }

    public async Task DeleteItem(IEpisode episode)
    {
        episode ??= ActiveItem;
        var index = Items.IndexOf(episode);
        if (index == -1)
        {
            return;
        }

        if (!DeleteUtil.AskForConfirmation(episode.Name))
        {
            return;
        }

        try
        {
            if (await episode.DeleteInDatabase())
            {
                Items.Remove(episode);
                DeselectItem();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed updating episode in database");
        }
    }

    public void DeselectItem()
    {
        ChangeActiveItem(null, false);
    }

    public void OpenExternalLink(Episode episode)
    {
        LinkUtil.Open(episode.Link);
    }

    public async Task OpenFolder(Episode episode)
    {
        episode.CanOpen = false;
        _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
        await Task.Delay(100);
        episode.CanOpen = true;
    }

    public async Task ToggleWatch(Episode episode)
    {
        if (episode == null)
        {
            return;
        }

        episode.Watched = !episode.Watched;
        episode.WatchedDate = episode.Watched ? DateTime.UtcNow : default;
        await episode.UpdateInDatabase();
    }

    public async Task Watch(Episode episode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(episode.File))
            {
                _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
                return;
            }

            await ProcessUtil.OpenFile(episode);

            episode.Watched = true;
            episode.WatchedDate = DateTime.UtcNow;
            await episode.UpdateInDatabase();
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed opening file to watch");
            MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                icon: MessageBoxImage.Error);
            await OpenFolder(episode);
        }
    }

    public async Task Download(Episode episode)
    {
        var (_, torrent) = await _torrentService.Download(episode);
        if (torrent is not null && string.IsNullOrWhiteSpace(episode.File))
        {
            episode.File ??= torrent.FileMode == TorrentFileMode.Single
                ? Path.Join(episode.Folder, torrent.File.FileName)
                : null;
            episode.TorrentId ??= torrent?.GetInfoHash();
            await episode.UpdateInDatabase();
        }
    }

    public async Task DeleteWithFile()
    {
        var episode = ActiveItem;

        if (!DeleteUtil.AskForConfirmation(episode.Name))
        {
            return;
        }

        var success = await _torrentService.Remove(episode, true);

        // wait 1 second for the torrent client to delete the file
        await Task.Delay(1.Seconds());

        if (!success)
        {
            return;
        }

        if (await episode.DeleteInDatabase())
        {
            Items.Remove(episode);
            DeselectItem();
        }

        if (!string.IsNullOrWhiteSpace(episode.File) && File.Exists(episode.File))
        {
            try
            {
                File.Delete(episode.File);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"failed deleting file {episode.File}");
            }
        }
    }

    protected override void OnInitialActivate()
    {
        _eventAggregator.Subscribe(this);

        _ = Task.Run(async () => await LoadEpisodes());
        _ = Task.Run(async () => await DownloadMissingCovers());
        _ = Task.Run(async () => await GetAiringEpisodesForToday());

        JobManager.AddJob(
            () => { _ = UpdateTorrents(); },
            s => s.WithName("Home:UpdateTorrents")
                  .NonReentrant()
                  .ToRunEvery(1)
                  .Seconds()
        );
#if DEBUG
        JobManager.AddJob(
            () => { GetActiveWindowTitle(); },
            s => s.WithName("Home:GetActiveWindowTitle")
                  .NonReentrant()
                  .ToRunEvery(10)
                  .Seconds()
        );

#endif
    }

    private async Task DownloadMissingCovers()
    {
        await using var db = new TrackContext();
        var rows = 0;
        try
        {
            // remove cover if files don't exist
            var covers = await db.Covers
                                 .Include(c => c.Animes)
                                 .Include(c => c.Episodes)
                                 .ToListAsync();
            foreach (var cover in covers)
            {
                if (File.Exists(cover.FilePath))
                {
                    continue;
                }

                //var anime = db.Anime.Where(a => a.CoverData.Id == cover.Id);

                db.Remove(cover);
                rows += await db.SaveChangesAsync();
                _logger.Information("removed cover id: {0}, file: {1}", cover.Id, cover.FilePath);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "error checking covers");
        }

        try
        {
            var animes = await db.Anime
                                 .Include(a => a.CoverData)
                                 .ToListAsync();
            foreach (var anime in animes)
            {
                anime.Created = anime.Created == default ? anime.Released : anime.Created;
                var coverData = anime.CoverData ?? await anime.Cover.GetCoverData(anime, _httpClient, _logger);
                anime.CoverData ??= coverData;
                var episodes = await db.Episodes
                                       .Include(e => e.CoverData)
                                       .Where(e => e.AnimeId == anime.GroupId)
                                       .ToListAsync();
                foreach (var episode in episodes)
                {
                    episode.CoverData ??= coverData;
                    episode.Created = episode.Created == default ? episode.Released : episode.Created;
                }

                rows += await db.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed getting coverData");
        }


        if (rows >= 1)
        {
            _logger.Information($"updated {rows} rows with CoverData");
        }
    }

    public async Task LoadEpisodes()
    {
        try
        {
            await using var db = new TrackContext();
            var episodes = await db.Episodes
                                   .Where(e => !e.Hide)
                                   .Include(e => e.CoverData)
                                   .OrderBy(e => e.Released)
                                   .ToListAsync();

            Items.Clear();
            await Task.Delay(100);

            foreach (var episode in episodes)
            {
                var anime = await db.Anime
                                    .Include(a => a.AniListAnime)
                                    .FirstOrDefaultAsync(a => a.GroupId == episode.AnimeId);
                if (anime is not null)
                {
                    episode.Anime = anime;
                }

                Items.Add(episode);
                await Task.Delay(1);
            }

            ChangeActiveItem(null, false);
#if DEBUG
            Items.Add(new Episode
            {
                Name = "test :: Episode 1",
                Released = DateTime.UtcNow,
                Folder = Directory.GetCurrentDirectory(),
            });
#endif
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed loading episodes");
        }
    }

    // this will create a crash log on closing the app
    //protected override void OnClose()
    //{
    //    _getTorrentsStatusTimer.Stop();
    //    _getTorrentsStatusTimer.Dispose();
    //}

    private async Task UpdateTorrents()
    {
        if (Items.Count <= 0 || _updatingTorrents)
        {
            return;
        }

        _updatingTorrents = true;
        try
        {
            await _torrentService.UpdateTorrentProgress(Items);
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed updating torrent progress");
        }
        finally
        {
            _updatingTorrents = false;
        }
    }

    public async Task GetAiringEpisodesForToday()
    {
        var animesToday = new List<FutureEpisode>();
        await using var db = new TrackContext();
        var animes = db.Anime.Where(a => a.Status == AnimeStatus.Watching);
        if (!animes.Any())
        {
            return;
        }

        foreach (var anime in animes)
        {
            var lastEpisode = await db.Episodes
                                      .Where(e => e.AnimeId == anime.GroupId)
                                      .OrderBy(e => e.Released)
                                      .LastOrDefaultAsync();

            if (lastEpisode == default)
            {
                continue;
            }

            var potentialNextRelease = lastEpisode.Released + TimeSpan.FromDays(7);
            if (potentialNextRelease.Date == DateTime.Today)
            {
                animesToday.Add(new FutureEpisode
                {
                    Name = anime.Name,
                    Date = potentialNextRelease,
                });
            }
        }


        AnimesToday.Clear();
        if (animesToday.Any())
        {
            AnimesToday.AddRange(animesToday.OrderBy(a => a.Date));
        }

#if DEBUG
        AnimesToday.Add(new FutureEpisode
        {
            Name = "Test",
            Date = DateTime.UtcNow - TimeSpan.FromHours(2),
        });
        AnimesToday.Add(new FutureEpisode
        {
            Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus ac.",
            Date = DateTime.UtcNow - TimeSpan.FromMinutes(5),
        });
#endif
    }

    public void SelectionChanged()
    {
        if (_settingsService.Settings.GroupHomeList) return;

        if (ActiveItem is null)
        {
            foreach (var episode in Items) episode.HomeHighlight = false;
            return;
        }

        foreach (var episode in Items)
            episode.HomeHighlight = episode.AnimeId == ActiveItem.AnimeId
                                    && episode.Id != ActiveItem.Id
                                    && ActiveItem.AnimeId != null;
    }

    public void TrackAnime()
    {
        // TODO make this prettier somehow...
        MainViewModel.Instance.ActiveItem = MainViewModel.Instance.Items[2];
    }

    public void ShowInformation(Episode episode)
    {
        if (episode?.Anime?.AniListAnime is null)
        {
            return;
        }

        var window = new AnimeInfoPanelWindow
        {
            DataContext = episode.Anime.AniListAnime,
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }


    public async Task EditAnime(Episode episode)
    {
        if (episode?.Anime is null)
        {
            return;
        }

        await using var db = new TrackContext();
        var episodes = db.Episodes
                         .Where(e => e.AnimeId == episode.Anime.GroupId);
        episode.Anime.EpisodeList = new BindableCollection<Episode>(episodes);

        _trackedAnimeEditContentViewModel.SetAnime(episode.Anime);
        _windowManager.ShowDialog(_trackedAnimeEditContentViewModel);
    }

    public void ListViewLoaded(object sender, RoutedEventArgs e)
    {
        if (!_settingsService.Settings.GroupHomeList || _listViewLoaded)
        {
            return;
        }

        var listView = (ListView)sender;
        listView.Items.GroupDescriptions?.Add(new PropertyGroupDescription("AnimeName"));

        _listViewLoaded = true;
    }


#if DEBUG
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private string GetActiveWindowTitle()
    {
        const int nChars = 256;
        var buff = new StringBuilder(nChars);
        var handle = GetForegroundWindow();

        if (GetWindowText(handle, buff, nChars) <= 0)
        {
            return null;
        }

        _logger.Debug($"current window -> {buff}");
        return buff.ToString();
    }
#endif
}