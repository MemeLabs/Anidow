using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
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
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TorrentService _torrentService;
        private ScrollViewer _scrollViewer;

        public AnimeBytesSearchViewModel(ILogger logger, IEventAggregator eventAggregator,
            AnimeBytesService animeBytesService, TorrentService torrentService, SettingsService settingsService)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _animeBytesService = animeBytesService;
            _torrentService = torrentService;
            _settingsService = settingsService;
            DisplayName = "Search";
        }

        public DateTime LastSearch { get; set; }
        public string Search { get; set; }

        public bool CanSearch => !string.IsNullOrWhiteSpace(_settingsService.Settings.AnimeBytesSettings.Username)
                                 && !string.IsNullOrWhiteSpace(
                                     _settingsService.Settings.AnimeBytesSettings.PassKey);

        public bool CanGetItems { get; set; } = true;

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
            var response = await _animeBytesService.SearchAnime(Search);
            if (response == default)
            {
                CanGetItems = true;
                return;
            }

            Items.Clear();
            Items.AddRange(response.Groups);

            _scrollViewer?.ScrollToTop();
            ActiveItem = null!;
            LastSearch = DateTime.Now;
            CanGetItems = true;
        }

        public void DeselectItem()
        {
            ChangeActiveItem(null, false);
        }

        public async Task Download(AnimeBytesScrapeAnime anime)
        {
            var torrent = anime?.SelectedTorrent;

            if (string.IsNullOrWhiteSpace(torrent?.Folder))
            {
                return;
            }

            var success = await _torrentService.Download(torrent);
            if (!success)
            {
                return;
            }

            _eventAggregator.PublishOnUIThread(new DownloadEvent
            {
                Item = anime
            });
        }

        public void OpenFolderBrowserDialog(AnimeBytesScrapeAnime anime)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = anime?.SelectedTorrent?.Folder ?? Directory.GetCurrentDirectory()
            };
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            foreach (var torrent in ActiveItem.Torrents) torrent.Folder = dialog.SelectedPath;
        }

        public void OpenExternalLink(AnimeBytesScrapeAnime item)
        {
            if (string.IsNullOrWhiteSpace(item.SeriesID))
            {
                return;
            }

            LinkUtil.Open($"https://animebytes.tv/series.php?id={item.SeriesID}");
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