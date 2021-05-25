using System;
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
using Anidow.Extensions;
using Anidow.Services;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using UserControl = System.Windows.Controls.UserControl;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TrackedViewModel : Conductor<Anime>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly IWindowManager _windowManager;
        private ScrollViewer[] _scrollViewers;
        private string _search;

        public TrackedViewModel(SettingsService settingsService, HttpClient httpClient, TaskbarIcon taskbarIcon,
            IEventAggregator eventAggregator, IWindowManager windowManager, ILogger logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            DisplayName = "Tracked";
        }

        public AnimeStatus FilterStatus { get; set; } = AnimeStatus.Watching;

        public string Search
        {
            get => _search;
            set
            {
                SetAndNotify(ref _search, value);
                Debouncer.DebounceAction("load_tracked",
                    async _ => { await DispatcherUtil.DispatchAsync(async () => await Load()); });
            }
        }

        public bool ViewToggle => _settingsService.Settings.TrackedIsCardView;


        public bool CanLoad { get; set; }

        protected override async void OnInitialActivate()
        {
            await Load();
        }

        public async Task Load()
        {
            CanLoad = false;
            await using var db = new TrackContext();
            var anime = await Task.Run(async () => await db.Anime
                                                           .Include(a => a.CoverData)
                                                           .OrderByDescending(a => a.Released)
                                                           .ToListAsync());

            if (!string.IsNullOrWhiteSpace(Search))
            {
                anime = anime.Where(a =>
                                 a.Name.Contains(_search, StringComparison.InvariantCultureIgnoreCase))
                             .ToList();
            }

            anime = FilterStatus switch
            {
                AnimeStatus.Watching => anime.Where(a => a.Status == AnimeStatus.Watching).ToList(),
                AnimeStatus.Finished => anime.Where(a => a.Status == AnimeStatus.Finished).ToList(),
                AnimeStatus.Dropped => anime.Where(a => a.Status == AnimeStatus.Dropped).ToList(),
                _ => anime,
            };

            ScrollToTop();
            Items.Clear();
            foreach (var a in anime)
            {
                a.Episodes = await db.Episodes.CountAsync(e => e.AnimeId == a.GroupId);
                await DispatcherUtil.DispatchAsync(() => Items.Add(a));
            }

            CanLoad = true;
        }

        private void ScrollToTop()
        {
            if (_scrollViewers == null)
            {
                return;
            }

            Array.ForEach(_scrollViewers, s => s.ScrollToTop());
        }

        public async Task Delete(Anime anime)
        {
            if (await anime.DeleteInDatabase())
            {
                Items.Remove(anime);
            }
        }

        public async Task SetToFinished(Anime anime)
        {
            if (anime.Status == AnimeStatus.Finished)
            {
                return;
            }

            anime.Status = AnimeStatus.Finished;
            await anime.UpdateInDatabase();
        }

        public async Task SetToAiring(Anime anime)
        {
            if (anime.Status == AnimeStatus.Watching)
            {
                return;
            }

            anime.Status = AnimeStatus.Watching;
            await anime.UpdateInDatabase();
        }

        public async Task SaveAnime(Anime anime)
        {
            await anime.UpdateInDatabase();
            anime.Notification = "Saved!";
            if (_settingsService.Settings.Notifications)
            {
                _taskbarIcon.ShowBalloonTip("Saved", anime.Name, BalloonIcon.Info);
            }

            Debouncer.DebounceAction($"save-anime-{anime.GroupId}",
                async _ => { await Task.Delay(2500).ContinueWith(_ => anime.Notification = string.Empty); });
        }


        public void OpenFolderBrowserDialog(Anime anime)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = anime.Folder,
            };
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            anime.Folder = dialog.SelectedPath;
        }

        public void OpenFolderFilesWindow(Anime anime)
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(ref anime, _eventAggregator, _logger));
        }

        public void EditAnime(object sender, MouseButtonEventArgs _)
        {
            var anime = (Anime) ((Border) sender).DataContext;
            if (ActiveItem != null)
            {
                ActiveItem.TrackedViewSelected = false;
            }

            anime.TrackedViewSelected = true;
            ChangeActiveItem(anime, false);
        }

        public void DeselectItem()
        {
            if (ActiveItem is null)
            {
                return;
            }

            ActiveItem.TrackedViewSelected = false;
            ActivateItem(null);
        }


        public void TrackedLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not UserControl listView)
            {
                return;
            }

            var scrollView = listView.FindVisualChildren<ScrollViewer>();
            if (scrollView == null)
            {
                return;
            }

            _scrollViewers = scrollView.ToArray();
        }

        public async Task DownloadCover((object url, object anime) data)
        {
            try
            {
                var anime = (Anime) data.anime;
                var url = (string) data.url;
                Uri.TryCreate(url, UriKind.Absolute, out var uri);
                if (uri == null)
                {
                    return;
                }

                anime.Cover = url;
                anime.CoverData = await url.GetCoverData(anime, _httpClient, _logger);
                await SaveAnime(anime);
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed downloading Cover");
            }
        }
    }
}