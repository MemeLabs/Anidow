using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Services;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Notifications.Wpf.Core;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using Screen = Stylet.Screen;

namespace Anidow.Pages.Components.Tracked
{
    public class TrackedAnimeEditContentViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly IWindowManager _windowManager;

        public TrackedAnimeEditContentViewModel(SettingsService settingsService, TaskbarIcon taskbarIcon,
            HttpClient httpClient, IWindowManager windowManager, IEventAggregator eventAggregator, ILogger logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Anime Anime { get; private set; }
        public BindableCollection<Episode> Episodes { get; set; }

        public bool CanSaveAnime { get; set; } = true;

        public void SetAnime(Anime anime)
        {
            Anime = anime;
        }

        protected override async void OnInitialActivate()
        {
            base.OnInitialActivate();
            await using var db = new TrackContext();
            var episodes = await db.Episodes.Where(e => e.AnimeId == Anime.GroupId).ToListAsync();
            Episodes = new BindableCollection<Episode>(episodes);
        }

        public async Task Delete()
        {
            if (await Anime.DeleteInDatabase())
            {
                Close();
                _eventAggregator.PublishOnUIThread(new TrackedDeleteAnimeEvent
                {
                    Anime = Anime,
                });
                await NotificationUtil.ShowUndoAsync(Anime.Name, "Deleted", async () =>
                {
                    await Anime.AddToDatabase(Anime.CoverData);
                    _eventAggregator.PublishOnUIThread(new TrackedRefreshEvent());
                }, null, TimeSpan.FromSeconds(10));
            }
        }

        public async Task SaveAnime()
        {
            if (string.IsNullOrWhiteSpace(Anime.Group))
            {
                await NotificationUtil.ShowAsync("Warning", "Group can not be empty!", NotificationType.Warning);
                return;
            }

            await Anime.UpdateInDatabase();
            if (_settingsService.Settings.Notifications)
            {
                _taskbarIcon.ShowBalloonTip("Saved", Anime.Name, BalloonIcon.Info);
            }
            else
            {
                await NotificationUtil.ShowAsync(Anime.Name, "Saved!", NotificationType.Success);
            }
        }


        public void OpenFolderBrowserDialog()
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Anime.Folder,
            };
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            Anime.Folder = dialog.SelectedPath;
        }

        public void OpenFolderFilesWindow()
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(Anime, _logger));
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
                await SaveAnime();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed downloading Cover");
            }
        }

        public void Close()
        {
            RequestClose();
        }


        public async Task Watch(Episode episode)
        {
            
            try
            {
                if (string.IsNullOrWhiteSpace(episode.File))
                {
                    await OpenFolder(episode);
                    return;
                }

                await ProcessUtil.OpenFile(episode);

                episode.Watched = true;
                episode.WatchedDate = DateTime.UtcNow;
                await episode.UpdateInDatabase();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed opening file to watch");
                MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                    icon: MessageBoxImage.Error);
                await OpenFolder(episode);
            }
        }

        public async Task OpenFolder(Episode episode)
        {
            episode.CanOpen = false;
            _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
            await Task.Delay(100);
            episode.CanOpen = true;
        }
    }
}