using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Extensions;
using Anidow.Interfaces;
using Anidow.Services;
using Anidow.Utils;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace Anidow.Pages
{
    public static class HistoryFilterType
    {
        public const string All = "All";
        public const string Watched = "Watched";
        public const string NotWatched = "Not watched";
    }

    public class HistoryViewModel : Conductor<IEpisode>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly int _maxFilesInView = 50;
        private readonly TorrentService _torrentService;
        private readonly IWindowManager _windowManager;

        private List<Episode> _episodes;
        private ScrollViewer _scrollViewer;
        private string _search;

        public HistoryViewModel(IEventAggregator eventAggregator, ILogger logger,
            IWindowManager windowManager, TorrentService torrentService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
            DisplayName = "History";
        }

        public string[] HistoryFilters => new[]
            {HistoryFilterType.All, HistoryFilterType.Watched, HistoryFilterType.NotWatched};

        public string FilterStatus { get; set; } = HistoryFilterType.All;

        public string Search
        {
            get => _search;
            set
            {
                SetAndNotify(ref _search, value);
                Debouncer.DebounceAction("load_history",
                    async _ => { await Execute.OnUIThreadAsync(async () => await LoadEpisodes(true)); });
            }
        }

        public string EpisodesLoaded { get; set; }

        public bool CanLoadMore { get; set; } = true;

        protected override async void OnInitialActivate()
        {
            await LoadEpisodes();
        }


        public async Task LoadEpisodes(bool clear = false)
        {
            await using var db = new TrackContext();
            var episodes = await db.Episodes.Where(e => e.Hide)
                .ToListAsync();


            if (!string.IsNullOrWhiteSpace(Search))
            {
                episodes = episodes.Where(a => a.Name.Contains(_search, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }


            episodes = FilterStatus switch
            {
                HistoryFilterType.Watched => episodes.Where(a => a.Watched).ToList(),
                HistoryFilterType.NotWatched => episodes.Where(a => !a.Watched).ToList(),
                _ => episodes,
            };

            _episodes = episodes
                .OrderByDescending(e => e.HideDate)
                .ThenByDescending(e => e.Released)
                .ToList();

            if (clear)
            {
                Items.Clear();
            }

            _scrollViewer?.ScrollToTop();
            LoadMore();
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

        public void LoadMore()
        {
            Items.AddRange(_episodes.Skip(Items.Count).Take(_maxFilesInView));
            CanLoadMore = Items.Count < _episodes.Count;
            EpisodesLoaded = $"{Items.Count}/{_episodes.Count}";
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
                _logger.Error(e, "failed opening file");
                MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                    icon: MessageBoxImage.Error);
                OpenFolder(episode);
            }
        }

        public async Task DeleteItem(Episode episode)
        {
            episode ??= (Episode)ActiveItem;
            var index = Items.IndexOf(episode);
            if (index == -1)
            {
                return;
            }

            try
            {
                await using var db = new TrackContext();
                db.Attach(episode);
                db.Remove(episode);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed deleting episode in database");
            }

            Items.Remove(episode);
            DeselectItem();
        }


        public async Task DeleteWithFile(Episode episode)
        {
            episode ??= (Episode)ActiveItem;
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

        public async Task UnWatchItem(Episode episode)
        {
            episode ??= (Episode)ActiveItem;
            var index = Items.IndexOf(episode);
            if (index == -1)
            {
                return;
            }

            episode.Hide = false;
            episode.HideDate = default;

            try
            {
                await episode.UpdateInDatabase();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed updating episode in database");
            }

            Items.Remove(episode);
        }

        public void OpenExternalLink(Episode episode)
        {
            LinkUtil.Open(episode.Link);
        }

        public void OpenFolder(Episode anime)
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(ref anime, _eventAggregator, _logger));
        }

        public void DeselectItem()
        {
            ChangeActiveItem(null, false);
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