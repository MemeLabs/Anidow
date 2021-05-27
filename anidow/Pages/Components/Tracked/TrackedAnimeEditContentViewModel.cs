using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdonisUI.Controls;
using Anidow.Database.Models;
using Anidow.Extensions;
using Anidow.Services;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using Screen = Stylet.Screen;

namespace Anidow.Pages.Components.Tracked
{
    public class TrackedAnimeEditContentViewModel : Screen
    {
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly HttpClient _httpClient;
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;

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

        public void SetAnime(Anime anime)
        {
            Anime = anime;
        }

        public async Task Delete()
        {
            if (await Anime.DeleteInDatabase())
            {
                Close();
            }
        }

        public bool CanSaveAnime { get; set; } = true;
        public async Task SaveAnime()
        {
            if (string.IsNullOrWhiteSpace(Anime.Group))
            {
                Anime.Notification = "Group can not be empty!";
                Debouncer.DebounceAction($"save-anime-{Anime.GroupId}-failed",
                    async _ => { await Task.Delay(2500).ContinueWith(_ => Anime.Notification = string.Empty); });
                return;
            }

            await Anime.UpdateInDatabase();
            Anime.Notification = "Saved!";
            if (_settingsService.Settings.Notifications)
            {
                _taskbarIcon.ShowBalloonTip("Saved", Anime.Name, BalloonIcon.Info);
            }

            Debouncer.DebounceAction($"save-anime-{Anime.GroupId}",
                async _ => { await Task.Delay(2500).ContinueWith(_ => Anime.Notification = string.Empty); });
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
            _windowManager.ShowWindow(new FolderFilesViewModel(Anime, _eventAggregator, _logger));
        }


        public async Task DownloadCover((object url, object anime) data)
        {
            try
            {
                var anime = (Anime)data.anime;
                var url = (string)data.url;
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
                    OpenFolder(episode);
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

        public void OpenFolder(Episode episode)
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _eventAggregator, _logger));
        }
    }
}
