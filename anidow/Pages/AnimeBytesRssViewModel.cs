using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
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
using Application = System.Windows.Application;
using ListView = System.Windows.Controls.ListView;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesRssViewModel : Conductor<AnimeBytesTorrentItem>.Collection.OneActive
    {
        private readonly AnimeBytesService _animeBytesService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TorrentService _torrentService;
        private ScrollViewer _scrollViewer;

        public AnimeBytesRssViewModel(ILogger logger, IEventAggregator eventAggregator,
            AnimeBytesService animeBytesService, TorrentService torrentService, SettingsService settingsService)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _animeBytesService = animeBytesService;
            _torrentService = torrentService;
            _settingsService = settingsService;
            DisplayName = "Rss feed";
        }
        public bool CanSearch => !string.IsNullOrWhiteSpace(_settingsService.GetSettings().AnimeBytesSettings.PassKey);
        public string LastSearch { get; set; }

        public string Filter
        {
            get => _filter;
            set
            {
                SetAndNotify(ref _filter, value);
                HighlightFoundItems(value);
            }
        }

        private void HighlightFoundItems(string value)
        {
            foreach (var item in Items)
            {
                item.ShowInList = item.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        public IReadOnlyList<string> Filters => new List<string>
        {
            "Anime torrents",
            "Airing anime torrents"
        };

        public int SelectedFilterIndex { get; set; }

        public bool CanGetItems { get; set; } = true;

        //public bool CanDownload => ActiveItem != null && !string.IsNullOrWhiteSpace(ActiveItem.Folder);

        protected override async void OnInitialActivate()
        {
            base.OnActivate();
            if (CanSearch)
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
                _ => throw new NotImplementedException()
            });
            if (items == default || items.Count <= 0)
            {
                CanGetItems = true;
                return;
            }

            Items.Clear();
            foreach (var animeBytesFeedItem in items)
            {
                if (SelectedFilterIndex == 1)
                {
                    animeBytesFeedItem.CanTrack = !tracked.Contains(animeBytesFeedItem.GroupId);
                }
                await Execute.OnUIThreadAsync(() => Items.Add(animeBytesFeedItem));
            }

            _scrollViewer?.ScrollToTop();
            _orderType = "DESC";
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
            _logger.Information($"downloading {item.Name}");
            var success = await _torrentService.Download(item);
            if (!success)
            {
                return;
            }

            _eventAggregator.PublishOnUIThread(new DownloadEvent
            {
                Item = item
            });
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
            var parts = item.TorrentProperty.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim())
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

            await db.Anime.AddAsync(anime);
            await db.SaveChangesAsync();
            item.CanTrack = false;
        }

        public void OpenFolderBrowserDialog()
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = ActiveItem.Folder
            };
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                ActiveItem.Folder = dialog.SelectedPath;
            }
        }


        private string _orderType = "DESC";
        private string _orderColumn = "Added";
        private string _filter;

        public void Sort(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header)
            {
                return;
            }

            var columnName = header.Column.Header.ToString();

            if (columnName != _orderColumn)
            {
                _orderType = "DESC";
            }

            var orderedItems = columnName switch
            {
                "Added" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i => i.Released).ToList(),
                    "ASC" => Items.OrderByDescending(i => i.Released).ToList(),
                    _ => Items.OrderByDescending(i => i.Released).ToList()
                },
                "Name" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i => i.Name).ToList(),
                    "ASC" => Items.OrderByDescending(i => i.Name).ToList(),
                    _ => Items.OrderByDescending(i => i.Name).ToList()
                },
                "Resolution" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i => i.Resolution).ToList(),
                    "ASC" => Items.OrderByDescending(i => i.Resolution).ToList(),
                    _ => Items.OrderByDescending(i => i.Resolution).ToList()
                },
                "Episode" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Episode) ? "0" : i.Episode)).ToList(),
                    "ASC" => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Episode) ? "0" : i.Episode)).ToList(),
                    _ => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Episode) ? "0" : i.Episode)).ToList(),
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
            _scrollViewer?.ScrollToTop();
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
