using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using ListView = System.Windows.Controls.ListView;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesRssViewModel : Conductor<AnimeBytesTorrentItem>.Collection.OneActive
    {
        private readonly AnimeBytesService _animeBytesService;
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TorrentService _torrentService;

        private string _filter;
        private ScrollViewer _scrollViewer;

        public AnimeBytesRssViewModel(ILogger logger, IEventAggregator eventAggregator, HttpClient httpClient,
            AnimeBytesService animeBytesService, TorrentService torrentService, SettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _animeBytesService = animeBytesService ?? throw new ArgumentNullException(nameof(animeBytesService));
            _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            DisplayName = "Rss feed";
        }

        public bool CanSearch => !string.IsNullOrWhiteSpace(_settingsService.Settings.AnimeBytesSettings.PassKey);
        public string LastSearch { get; private set; }

        public string Filter
        {
            get => _filter;
            set
            {
                SetAndNotify(ref _filter, value);
                HighlightFoundItems(value);
            }
        }

        public IReadOnlyList<string> Filters => new List<string>
        {
            "Anime torrents",
            "Airing anime torrents",
        };

        public int SelectedFilterIndex { get; set; }

        public bool CanGetItems { get; set; } = true;

        private void HighlightFoundItems(string value)
        {
            foreach (var item in Items)
                item.ShowInList = item.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase);
        }

        //public bool CanDownload => ActiveItem != null && !string.IsNullOrWhiteSpace(ActiveItem.Folder);

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
            await using var db = new TrackContext();
            var tracked = await db.Anime.Select(a => a.GroupId).ToListAsync();

            var items = await Task.Run(async () => SelectedFilterIndex switch
            {
                0 => await _animeBytesService.GetFeedItems(AnimeBytesFilter.All),
                1 => await _animeBytesService.GetFeedItems(AnimeBytesFilter.Airing),
                _ => throw new NotImplementedException(),
            });
            if (items is not {Count: > 0})
            {
                CanGetItems = true;
                return;
            }

            Items.Clear();
            foreach (var item in items)
            {
                item.CanTrack = !tracked.Contains(item.GroupId)
                                && !string.IsNullOrWhiteSpace(item.Resolution)
                                && !string.IsNullOrWhiteSpace(item.GetReleaseGroup());

                await DispatcherUtil.DispatchAsync(() => Items.Add(item));
            }

            _scrollViewer?.ScrollToTop();
            ActiveItem = null!;
            LastSearch = $"{DateTime.Now:T}";
            CanGetItems = true;
        }

        public void DeselectItem()
        {
            ChangeActiveItem(null, false);
        }

        public void OpenExternalLink(AnimeBytesTorrentItem item)
        {
            LinkUtil.Open(item.GroupUrl);
        }

        public async Task Download(AnimeBytesTorrentItem item)
        {
            item.CanDownload = false;
            _logger.Information($"downloading {item.Name}");
            var (success, torrent) = await _torrentService.Download(item);
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

        public async Task Track(AnimeBytesTorrentItem item)
        {
            await using var db = new TrackContext();
            var isTracking = await db.Anime.FirstOrDefaultAsync(a => a.GroupId == item.GroupId);
            if (isTracking != null)
            {
                return;
            }

            var resolution = string.Empty;
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries)
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

            try
            {
                Directory.CreateDirectory(item.Folder);
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed creating directory");
                return;
            }

            var anime = new Anime
            {
                Site = Site.AnimeBytes,
                Cover = item.Cover,
                Folder = item.Folder,
                GroupId = item.GroupId,
                Name = item.GroupTitle,
                GroupUrl = item.GroupUrl,
                Released = item.Released,
                Resolution = resolution,
                Group = item.GetReleaseGroup(),
                Status = AnimeStatus.Watching,
            };
            anime.CoverData = await anime.Cover.GetCoverData(anime, _httpClient, _logger);

            await db.Anime.AddAsync(anime);
            await db.SaveChangesAsync();
            item.CanTrack = false;
        }


        public void OpenFolderBrowserDialog()
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = ActiveItem.Folder,
            };
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                ActiveItem.Folder = dialog.SelectedPath;
            }
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