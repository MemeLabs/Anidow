using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Pages.Components.Settings;
using Anidow.Services;
using Anidow.Utils;
using BencodeNET.Torrents;
using FluentScheduler;
using Hardcodet.Wpf.TaskbarNotification;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class HomeViewModel : Conductor<Episode>.Collection.OneActive, IHandle<DownloadEvent>,
        IHandle<RefreshHomeEvent>, IHandle<AddToHomeEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly SettingsSetupWizardViewModel _settingsSetupWizardViewModel;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly TorrentService _torrentService;
        private readonly IWindowManager _windowManager;


        public HomeViewModel(IEventAggregator eventAggregator, ILogger logger,
            IWindowManager windowManager, AnimeBytesService animeBytesService,
            TorrentService torrentService, SettingsService settingsService,
            SettingsSetupWizardViewModel settingsSetupWizardViewModel,
            TaskbarIcon taskbarIcon, HttpClient httpClient)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            AnimeBytesService = animeBytesService ?? throw new ArgumentNullException(nameof(animeBytesService));
            _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settingsSetupWizardViewModel = settingsSetupWizardViewModel ??
                                            throw new ArgumentNullException(nameof(settingsSetupWizardViewModel));
            DisplayName = "Home";
        }

        public AnimeBytesService AnimeBytesService { get; set; }
        public bool CanForceCheck { get; set; } = true;
        public string NextCheckIn { get; private set; } = "00:00";
        public Timer NextCheckTimer { get; set; }
        public IObservableCollection<FutureEpisode> AnimesToday { get; set; } = new BindableCollection<FutureEpisode>();
        public bool IsEnabled => _settingsService != null && !_settingsService.Settings.FirstStart;

        public async void Handle(AddToHomeEvent message)
        {
            await DispatcherUtil.DispatchAsync(() => Items.Add(message.Episode));
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
            switch (message.Item)
            {
                case AnimeBytesTorrentItem abti:
                {
                    var anime = await db.Anime.FirstOrDefaultAsync(a => a.GroupId == abti.GroupId);
                    if (anime != null)
                    {
                        item.AnimeId = anime.GroupId;
                        item.CoverData ??= anime.CoverData;
                    }

                    break;
                }
                case AnimeBytesScrapeAnime absa:
                {
                    var anime = await db.Anime.FirstOrDefaultAsync(a => a.GroupId == absa.ID.ToString());
                    if (anime != null)
                    {
                        item.AnimeId = anime.GroupId;
                        item.CoverData ??= anime.CoverData;
                    }

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

        private void NextCheckTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (!AnimeBytesService.TrackerIsRunning)
            {
                return;
            }

            var lastCheck = AnimeBytesService.LastCheck;
            var nextCheck = lastCheck + TimeSpan.FromMinutes(_settingsService.Settings.RefreshTime);
            NextCheckIn = $"{nextCheck - DateTime.Now:mm\\:ss}";
        }

        public async Task ForceCheck()
        {
            CanForceCheck = false;
            await AnimeBytesService.CheckForNewEpisodes();
            await GetAiringEpisodesForToday();
            CanForceCheck = true;
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

        public async Task HideItem(Episode episode)
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

        public async Task DeleteItem(Episode episode)
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

        public void OpenFolder(Episode episode)
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
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

                ProcessUtil.OpenFile(episode.File);

                episode.Watched = true;
                episode.WatchedDate = DateTime.UtcNow;
                await episode.UpdateInDatabase();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed opening file to watch");
                MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                    icon: MessageBoxImage.Error);
                OpenFolder(episode);
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

            NextCheckTimer = new Timer(100);
            NextCheckTimer.Elapsed += NextCheckTimerOnElapsed;
            NextCheckTimer.Start();

            Task.Run(async () => await LoadEpisodes());
            Task.Run(async () => await DownloadMissingCovers());
            Task.Run(async () => await GetAiringEpisodesForToday());

            // Starting the tracker
            if (_settingsService.Settings.StartTrackerAnimeBytesOnLaunch)
            {
                AnimeBytesService.InitTracker();
            }

            JobManager.AddJob(
                () => { UpdateTorrents().Wait(); },
                s => s.WithName("Home:UpdateTorrents")
                      .NonReentrant()
                      .ToRunEvery(1)
                      .Seconds()
            );
#if DEBUG
            ShowSetupWizard().ConfigureAwait(false);
#endif
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

                DispatcherUtil.DispatchSync(() => _windowManager.ShowDialog(_settingsSetupWizardViewModel));
            }
        }

        private async Task DownloadMissingCovers()
        {
            await using var db = new TrackContext();
            var rows = 0;
            try
            {
                // remove cover if files don't exist
                foreach (var cover in db.Covers.Include(c => c.Animes).Include(c => c.Episodes))
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
                // var animes = await db.Anime.ToListAsync();
                foreach (var anime in db.Anime)
                {
                    anime.Created = anime.Created == default ? anime.Released : anime.Created;
                    var coverData = anime.CoverData ?? await anime.Cover.GetCoverData(anime, _httpClient, _logger);
                    anime.CoverData ??= coverData;
                    var episodes = db.Episodes.Where(e => e.AnimeId == anime.GroupId);
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
            await using var db = new TrackContext();
            var episodes = db.Episodes.Where(e => !e.Hide)
                             .Include(e => e.CoverData)
                             .OrderBy(e => e.Released);

            Items.Clear();
            foreach (var episode in episodes) await DispatcherUtil.DispatchAsync(() => Items.Add(episode));
            ActiveItem = null;
#if DEBUG
            Items.Add(new Episode
            {
                Name = "test :: Episode 1",
                Released = DateTime.UtcNow,
                Folder = Directory.GetCurrentDirectory(),
            });
#endif
        }

        // this will create a crash log on closing the app
        //protected override void OnClose()
        //{
        //    _getTorrentsStatusTimer.Stop();
        //    _getTorrentsStatusTimer.Dispose();
        //}

        private async Task UpdateTorrents()
        {
            if (Items.Count <= 0)
            {
                return;
            }

            try
            {
                await _torrentService.UpdateTorrentProgress(Items);
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating torrent progress");
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
    }
}