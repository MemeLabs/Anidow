using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using Microsoft.EntityFrameworkCore;
using Notifications.Wpf.Core;
using Serilog;
using Stylet;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListView = System.Windows.Controls.ListView;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesSearchViewModel : Conductor<AnimeBytesScrapeAnime>.Collection.OneActive
    {
        private readonly AnimeBytesService _animeBytesService;
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TorrentService _torrentService;
        private ScrollViewer _scrollViewer;

        public AnimeBytesSearchViewModel(ILogger logger, IEventAggregator eventAggregator, HttpClient httpClient,
            AnimeBytesService animeBytesService, TorrentService torrentService, SettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _animeBytesService = animeBytesService ?? throw new ArgumentNullException(nameof(animeBytesService));
            _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            DisplayName = "Search";
        }

        public DateTime LastSearch { get; set; }
        public string Search { get; set; }

        public bool CanSearch => !string.IsNullOrWhiteSpace(
                                     _settingsService.Settings.AnimeBytesSettings.Username)
                                 && !string.IsNullOrWhiteSpace(
                                     _settingsService.Settings.AnimeBytesSettings.PassKey);

        public bool CanGetItems { get; set; } = true;

        protected override async void OnActivate()
        {
            if (CanSearch && Items.Count <= 0)
            {
                await GetItems();
            }
        }

        public async Task GetItems()
        {
            CanGetItems = false;
            var response = await _animeBytesService.SearchAnime(Search);
            if (response == default)
            {
                CanGetItems = true;
                return;
            }

            await using var db = new TrackContext();
            var tracked = await db.Anime.Select(a => a.GroupId).ToListAsync();

            Items.Clear();
            foreach (var anime in response.Groups)
            {
                anime.Folder = _settingsService.Settings.AnimeFolder;
                anime.CanTrack = !tracked.Contains($"{anime.ID}")
                                 && anime.SubGroups.Count > 0
                                 && anime.GroupName.Equals("TV Series", StringComparison.InvariantCultureIgnoreCase);

                foreach (var torrent in anime.Torrents) torrent.Folder = anime.Folder;
                await DispatcherUtil.DispatchAsync(() => Items.Add(anime));
            }

            _scrollViewer?.ScrollToTop();
            DeselectItem();
            LastSearch = DateTime.Now;
            CanGetItems = true;
        }

        public async Task Track(AnimeBytesScrapeAnime item)
        {
            await using var db = new TrackContext();
            var isTracking = await db.Anime.FirstOrDefaultAsync(a => a.GroupId == $"{item.ID}");
            if (isTracking != null || string.IsNullOrWhiteSpace(item.SelectedSubGroup))
            {
                return;
            }

            var resolution = string.Empty;
            var parts = item.SelectedSubGroup.Split('|', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();
            if (parts.Contains("1080p"))
            {
                resolution = "1080p";
            }

            if (parts.Contains("720p"))
            {
                resolution = "720p";
            }

            var group = parts[0];

            try
            {
                if (!Directory.Exists(item.Folder))
                {
                    Directory.CreateDirectory(item.Folder);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed creating directory");
                await NotificationUtil.ShowAsync("Error", $"failed creating directory:\n {item.Folder}",
                    NotificationType.Error);
                return;
            }

            var anime = new Anime
            {
                Site = Site.AnimeBytes,
                Cover = item.Image,
                Folder = item.Folder,
                GroupId = item.ID.ToString(),
                Name = item.FullName,
                GroupUrl = $"https://animebytes.tv/torrents.php?id={item.ID}",
                Released = default,
                Resolution = resolution,
                Group = group,
                Status = AnimeStatus.Watching,
            };
            anime.CoverData = await anime.Cover.GetCoverData(anime, _httpClient, _logger);

            await db.Anime.AddAsync(anime);
            await db.SaveChangesAsync();
            item.CanTrack = false;
        }

        public void DeselectItem()
        {
            ChangeActiveItem(null, false);
        }

        public async Task Download(AnimeBytesScrapeAnime item)
        {
            var selectedTorrent = item.SelectedTorrent;

            if (string.IsNullOrWhiteSpace(selectedTorrent.Folder))
            {
                return;
            }

            item.CanDownload = false;
            
            var (success, torrent) = await _torrentService.Download(selectedTorrent);
            if (success)
            {
                _eventAggregator.PublishOnUIThread(new DownloadEvent
                {
                    Item = item,
                    Torrent = torrent,
                });
            }

            await Task.Delay(100);
            item.CanDownload = true;
        }

        public void OpenFolderBrowserDialog(AnimeBytesScrapeAnime item)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = item.Folder ?? Directory.GetCurrentDirectory(),
            };
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            item.Folder = dialog.SelectedPath;
            foreach (var torrent in item.Torrents) torrent.Folder = dialog.SelectedPath;
        }

        public void OpenExternalLink(AnimeBytesScrapeAnime item)
        {
            LinkUtil.Open($"https://animebytes.tv/torrents.php?id={item.ID}");
        }

        public void OpenLink(string link)
        {
            LinkUtil.Open(link);
        }

        public async void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            await GetItems().ConfigureAwait(false);
            e.Handled = true;
        }

        public void ListLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListView listView || _scrollViewer != null)
            {
                return;
            }

            var scrollView = listView.FindVisualChildren<ScrollViewer>().FirstOrDefault();
            if (scrollView == null)
            {
                return;
            }

            _scrollViewer ??= scrollView;
        }
    }
}