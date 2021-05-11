using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Interfaces;
using Anidow.Model;
using Anidow.Services;
using Anidow.Torrent_Clients;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;
using Timer = System.Timers.Timer;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MainViewModel : Conductor<IEpisode>.Collection.OneActive, IHandle<DownloadEvent>,
        IHandle<RefreshHomeEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IWindowManager _windowManager;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly HttpClient _httpClient;
        private readonly TorrentService _torrentService;
        private Timer _getTorrentsStatusTimer;


        public MainViewModel(IEventAggregator eventAggregator, ILogger logger,
            IWindowManager windowManager, AnimeBytesService animeBytesService,
            TorrentService torrentService, SettingsService settingsService,
            TaskbarIcon taskbarIcon, HttpClient httpClient)
        {
            _eventAggregator = eventAggregator;
            _logger = logger;
            _windowManager = windowManager;
            AnimeBytesService = animeBytesService;
            _torrentService = torrentService;
            _settingsService = settingsService;
            _taskbarIcon = taskbarIcon;
            _httpClient = httpClient;
            DisplayName = "Home";
            eventAggregator.Subscribe(this);
        }

        public AnimeBytesService AnimeBytesService { get; set; }
        public bool CanForceCheck { get; set; } = true;
        public string NextCheckIn { get; set; }
        public Timer NextCheckTimer { get; set; }
        public IObservableCollection<FutureEpisode> AnimesToday { get; set; } = new BindableCollection<FutureEpisode>();
        
        private void NextCheckTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (!AnimeBytesService.TrackerIsRunning)
            {
                return;
            }
            var lastCheck = AnimeBytesService.LastCheck;
            var nextCheck = lastCheck + TimeSpan.FromMinutes(_settingsService.GetSettings().RefreshTime);
            NextCheckIn = $"next check in {nextCheck - DateTime.Now:mm\\:ss} min";
        }

        public async Task ForceCheck()
        {
            CanForceCheck = false;
            _logger.Verbose("Test");
            await AnimeBytesService.CheckForNewEpisodes();
            await GetAiringEpisodesForToday();
            CanForceCheck = true;
        }

        public async void Handle(DownloadEvent message)
        {
            var item = message.Item switch
            {
                NyaaTorrentItem nyaa => new Episode
                {
                    Name = nyaa.Name,
                    Site = Site.Nyaa,
                    Released = nyaa.Released,
                    File = Path.Join(nyaa.Folder, nyaa.Name),
                    Folder = nyaa.Folder,
                    Link = nyaa.Link,
                    DownloadLink = nyaa.DownloadLink
                },
                AnimeBytesTorrentItem ab => new Episode
                {
                    Name = ab.Name,
                    Site = Site.AnimeBytes,
                    Released = ab.Released,
                    Folder = ab.Folder,
                    Link = ab.GroupUrl,
                    DownloadLink = ab.DownloadLink,
                    Cover = ab.Cover
                },
                AnimeBytesScrapeAnime ab => new Episode
                {
                    Name = CreatePropertyEpisode(ab),
                    Site = Site.AnimeBytes,
                    Released = DateTime.Parse(ab.SelectedTorrent.UploadTime),
                    Folder = ab.SelectedTorrent.Folder,
                    Link = $"https://animebytes.tv/torrent/{ab.SelectedTorrent.ID}/group",
                    DownloadLink = ab.SelectedTorrent.DownloadLink,
                    Cover = ab.Image
                },
                _ => throw new NotSupportedException(nameof(message.Item))
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

            if (_settingsService.GetSettings().Notifications)
            {
                _taskbarIcon.ShowBalloonTip("Added", item.Name, BalloonIcon.None);
            }
            
            await db.AddAsync(item);
            await db.SaveChangesAsync();
            Items.Add(item);
            await GetAiringEpisodesForToday();
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

        public async void Handle(RefreshHomeEvent _)
        {
            await LoadEpisodes();
            await GetAiringEpisodesForToday();
        }

        public async Task DeleteItem(Episode episode)
        {
            episode ??= (Episode) ActiveItem;
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

        public void OpenExternalLink(Episode anime)
        {
            LinkUtil.Open(anime.Link);
        }

        public void OpenFolder(Episode anime)
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(ref anime, _eventAggregator, _logger));
            //try
            //{
            //    ProcessUtil.OpenFolder(anime.Folder);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e, "failed opening folder");
            //}
        }

        public async Task ToggleWatch(Episode anime)
        {
            if (anime == null)
            {
                return;
            }

            anime.Watched = !anime.Watched;
            anime.WatchedDate = anime.Watched ? DateTime.UtcNow : default;
            await anime.UpdateInDatabase();
        }

        public async Task Watch(Episode anime)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(anime.File))
                {
                    _windowManager.ShowWindow(new FolderFilesViewModel(ref anime, _eventAggregator, _logger));
                    return;
                }

                ProcessUtil.OpenFile(anime.File);

                anime.Watched = true;
                anime.WatchedDate = DateTime.UtcNow;
                await anime.UpdateInDatabase();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed opening file to watch");
            }
        }

        public async Task Download(Episode anime)
        {
            await _torrentService.Download(anime);
        }

        public async Task DeleteWithFile()
        {
            var anime = (Episode) ActiveItem;
            var result = MessageBox.Show($"are you sure you want to delete the file?\n\n{anime.Name}", "Delete",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            Items.Remove(anime);

            await using var db = new TrackContext();
            db.Attach(anime);
            db.Remove(anime);
            await db.SaveChangesAsync();

            var success = await _torrentService.Remove(anime, true);

            // wait 1 second for the torrent client to delete the file
            await Task.Delay(1.Seconds());

            if (success && !File.Exists(anime.File))
            {
                return;
            }

            try
            {
                File.Delete(anime.File);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"failed deleting file {anime.File}");
            }
        }

        protected override void OnInitialActivate()
        {
            _getTorrentsStatusTimer = new Timer {Interval = 5000};
            _getTorrentsStatusTimer.Elapsed += async (_, _) =>
            {
                await UpdateTorrents();
            };
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
            var animes = await db.Anime.ToListAsync();
            var rows = 0;
            foreach (var anime in animes)
            {
                var coverData = anime.CoverData ?? await anime.Cover.GetCoverData(anime, _httpClient, _logger);
                anime.CoverData ??= coverData;
                var episodes = db.Episodes.Where(e => e.AnimeId == anime.GroupId);
                foreach (var episode in episodes)
                {
                    episode.CoverData ??= coverData;
                }
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
            
            _orderType = "DESC";
            Items.Clear();
            Items.AddRange(episodes.OrderBy(e => e.Released));
            ActiveItem = null;
#if DEBUG
            Items.Add(new Episode
            {
                Name = "test :: Episode 1",
                Released = DateTime.Today,
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
                    anime.TorrentId = _settingsService.GetSettings().TorrentClient switch
                    {
                        TorrentClient.QBitTorrent => list.Select(j => j.ToObject<QBitTorrentEntry>())
                            .FirstOrDefault(i => i.name == anime1.Name)
                            ?.hash,
                        _ => anime.TorrentId
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
                        Date = potentialNextRelease.Humanize()
                    });
                }
            }


            AnimesToday.Clear();
            if (animesToday.Any()) AnimesToday.AddRange(animesToday);
#if DEBUG
            AnimesToday.Add(new FutureEpisode
            {
                Name = "Test0",
                Date = DateTime.Now.Humanize()
            });
            AnimesToday.Add(new FutureEpisode
            {
                Name = "Test1",
                Date = DateTime.Now.Humanize()
            });
#endif
        }
        

        private string _orderType = "DESC";
        private string _orderColumn = "Added";

        public void Sort(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header)
            {
                return;
            }

            var columnName = header.Column.Header.ToString();

            if (columnName == "Added")
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return;
            }

            if (columnName != _orderColumn)
            {
                _orderType = "DESC";
            }

            var orderedItems = columnName switch
            {
                "Name" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i => i.Name).ToList(),
                    "ASC" => Items.OrderByDescending(i => i.Name).ToList(),
                    _ => Items.OrderByDescending(i => i.Name).ToList()
                },
                "Episode" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.EpisodeNum) ? "0" : i.EpisodeNum)).ToList(),
                    "ASC" => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.EpisodeNum) ? "0" : i.EpisodeNum)).ToList(),
                    _ => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.EpisodeNum) ? "0" : i.EpisodeNum)).ToList(),
                },
                _ => Items.ToList(),
            };

            _orderColumn = columnName;
            _orderType = _orderType switch
            {
                "DESC" => "ASC",
                "ASC" => "DESC",
                _ => "DESC"
            };

            Items.Clear();
            Items.AddRange(orderedItems);
            e.Handled = true;
        }
    }

    public class FutureEpisode
    {
        public string Name { get; set; }
        public string Date { get; set; }
    }
}