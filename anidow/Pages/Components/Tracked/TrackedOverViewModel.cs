using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Services;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Anidow.Pages.Components.Tracked
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TrackedOverViewModel : Conductor<Anime>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly TrackedAnimeEditContentViewModel _trackedAnimeEditContentViewModel;
        private readonly IWindowManager _windowManager;
        private ScrollViewer[] _scrollViewers;
        private string _search;

        public TrackedOverViewModel(SettingsService settingsService, HttpClient httpClient, TaskbarIcon taskbarIcon,
            TrackedAnimeEditContentViewModel trackedAnimeEditContentViewModel,
            IEventAggregator eventAggregator, IWindowManager windowManager, ILogger logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
            _trackedAnimeEditContentViewModel = trackedAnimeEditContentViewModel ??
                                                throw new ArgumentNullException(
                                                    nameof(trackedAnimeEditContentViewModel));
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

        public bool ViewToggle { get; set; }


        public bool CanLoad { get; set; }

        protected override async void OnInitialActivate()
        {
            ViewToggle = _settingsService.Settings.TrackedIsCardView;
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
                a.EpisodeList = await db.Episodes.Where(e => e.AnimeId == a.GroupId).ToListAsync();
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

        protected override void OnDeactivate()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void CardEditAnime(object sender, MouseButtonEventArgs _)
        {
            var anime = (Anime)((Border)sender).DataContext;
            ChangeActiveItem(anime, false);
            _eventAggregator.Publish(new OpenAnimeEditEvent{Anime = anime});
        }

        public void ListEditAnime(Anime anime)
        {
            ChangeActiveItem(anime, false);
            _eventAggregator.Publish(new OpenAnimeEditEvent{Anime = anime});
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
    }
}
