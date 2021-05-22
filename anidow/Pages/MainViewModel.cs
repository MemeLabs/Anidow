using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Services;
using Anidow.Torrent_Clients;
using Anidow.Utils;
using BencodeNET.Torrents;
using Hardcodet.Wpf.TaskbarNotification;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MainViewModel : Conductor<Episode>.Collection.OneActive, IHandle<DownloadEvent>,
        IHandle<RefreshHomeEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly TorrentService _torrentService;
        private readonly IWindowManager _windowManager;
        private Timer _getTorrentsStatusTimer;


        public MainViewModel(IEventAggregator eventAggregator, ILogger logger,
            IWindowManager windowManager, AnimeBytesService animeBytesService,
            TorrentService torrentService, SettingsService settingsService,
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
            DisplayName = "Home";
            eventAggregator.Subscribe(this);
        }

        public AnimeBytesService AnimeBytesService { get; set; }
        public bool CanForceCheck { get; set; } = true;
        public string NextCheckIn { get; private set; } = "00:00";
        public Timer NextCheckTimer { get; set; }
        public IObservableCollection<FutureEpisode> AnimesToday { get; set; } = new BindableCollection<FutureEpisode>();

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
                    File = torrent?.FileMode == TorrentFileMode.Single ?
                        Path.Join(nyaa.Folder, torrent.File.FileName) : null,
                    Folder = nyaa.Folder,
                    Link = nyaa.Link,
                    DownloadLink = nyaa.DownloadLink,
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
                    File = torrent?.FileMode == TorrentFileMode.Single ?
                        Path.Join(ab.Folder, torrent.File.FileName) : null,
                },
                AnimeBytesScrapeAnime ab => new Episode
                {
                    Name = CreatePropertyEpisode(ab),
                    Site = Site.AnimeBytes,
                    Released = DateTime.Parse(ab.SelectedTorrent.UploadTime),
                    Folder = ab.SelectedTorrent.Folder,
                    Link = $"https://animebytes.tv/torrent/{ab.SelectedTorrent.ID}/group",
                    DownloadLink = ab.SelectedTorrent.DownloadLink,
                    Cover = ab.Image,
                    File = torrent?.FileMode == TorrentFileMode.Single ?
                        Path.Join(ab.SelectedTorrent.Folder, torrent.File.FileName) : null,
                },
                _ => throw new NotSupportedException(nameof(message.Item)),
            };

            await using var db = new TrackContext();
            if (message.Item is AnimeBytesTorrentItem ab1)
            {
                var anime = await db.Anime.FirstOrDefaultAsync(a => a.GroupId == ab1.GroupId);
                if (anime != null)
                {
                    item.AnimeId = anime.GroupId;
                    item.CoverData ??= anime.CoverData;
                }
            }

            if (_settingsService.Settings.Notifications)
            {
                _taskbarIcon.ShowBalloonTip("Added", item.Name, BalloonIcon.None);
            }

            await db.AddAsync(item);
            await db.SaveChangesAsync();
            Items.Add(item);
            await GetAiringEpisodesForToday();
        }

        public async void Handle(RefreshHomeEvent _)
        {
            await LoadEpisodes();
            await GetAiringEpisodesForToday();
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

        public async Task DeleteItem(Episode episode)
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
            _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _eventAggregator, _logger));
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
                    _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _eventAggregator, _logger));
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
            }
        }

        public async Task DeleteWithFile()
        {
            var episode = ActiveItem;
            var result = MessageBox.Show($"are you sure you want to delete the file?\n\n{episode.Name}", "Delete",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            Items.Remove(episode);

            await using var db = new TrackContext();
            db.Attach(episode);
            db.Remove(episode);
            await db.SaveChangesAsync();

            var success = await _torrentService.Remove(episode, true);

            // wait 1 second for the torrent client to delete the file
            await Task.Delay(1.Seconds());

            if (success && !File.Exists(episode.File))
            {
                return;
            }

            try
            {
                File.Delete(episode.File);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"failed deleting file {episode.File}");
            }
        }

        protected override void OnInitialActivate()
        {
            _getTorrentsStatusTimer = new Timer { Interval = 5000 };
            _getTorrentsStatusTimer.Elapsed += async (_, _) => { await UpdateTorrents(); };
            _getTorrentsStatusTimer.Start();

            NextCheckTimer = new Timer(100);
            NextCheckTimer.Elapsed += NextCheckTimerOnElapsed;
            NextCheckTimer.Start();

            _ = DownloadMissingCovers();
            _ = GetAiringEpisodesForToday();
        }

        private async Task DownloadMissingCovers()
        {
            await using var db = new TrackContext();

            var rows = 0;
            // remove cover if files don't exist
            foreach (var cover in db.Covers)
            {
                if (File.Exists(cover.FilePath))
                {
                    continue;
                }

                db.Remove(cover);
                rows += await db.SaveChangesAsync();
                _logger.Information("removed cover id: {0}, file: {1}", cover.Id, cover.FilePath);
            }

            var animes = await db.Anime.ToListAsync();
            foreach (var anime in animes)
            {
                var coverData = anime.CoverData ?? await anime.Cover.GetCoverData(anime, _httpClient, _logger);
                anime.CoverData ??= coverData;
                var episodes = db.Episodes.Where(e => e.AnimeId == anime.GroupId);
                foreach (var episode in episodes) episode.CoverData ??= coverData;

                rows += await db.SaveChangesAsync();
            }

            if (rows >= 1)
            {
                _logger.Information($"updated {rows} rows with CoverData");
            }
        }

        protected override async void OnActivate()
        {
            await LoadEpisodes();
        }

        private async Task LoadEpisodes()
        {
            await using var db = new TrackContext();
            var episodes = await db.Episodes.Where(e => !e.Hide)
                .Include(e => e.CoverData)
                .ToListAsync();

            Items.Clear();
            Items.AddRange(episodes.OrderBy(e => e.Released));
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

            var list = await _torrentService.GetTorrents<JsonElement>();

            foreach (var anime in Items)
                try
                {
                    var anime1 = anime;
                    anime.TorrentId = _settingsService.Settings.TorrentClient switch
                    {
                        TorrentClient.QBitTorrent => list.Select(j => j.ToObject<QBitTorrentEntry>())
                            .FirstOrDefault(i => i.name == anime1.Name)
                            ?.hash,
                        _ => anime.TorrentId,
                    };
                }
                catch (Exception)
                {
                    // hmmm
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
                AnimesToday.AddRange(animesToday);
            }
#if DEBUG
            AnimesToday.Add(new FutureEpisode
            {
                Name = "Test0",
                Date = DateTime.UtcNow,
            });
            AnimesToday.Add(new FutureEpisode
            {
                Name =
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus ac.",
                Date = DateTime.UtcNow,
            });
#endif
        }
    }

    public class FutureEpisode
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateLocal => Date.ToLocalTime();
        public string DateString => Date.Humanize();
    }
}