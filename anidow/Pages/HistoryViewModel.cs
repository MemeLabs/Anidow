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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anidow.Enums;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace Anidow.Pages
{
    public enum Filter
    {
        All,
        Watched,
        NotWatched
    }
    public class HistoryViewModel : Conductor<IEpisode>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IWindowManager _windowManager;
        private readonly TorrentService _torrentService;
        private string _search;
        
        public Filter FilterStatus { get; set; } = Filter.All;

        public string Search
        {
            get => _search;
            set
            {
                SetAndNotify(ref this._search, value);
                Debouncer.DebounceAction("load_history", async config =>
                {
                    await LoadEpisodes();
                });
            }
        }

        public HistoryViewModel(IEventAggregator eventAggregator, ILogger logger,
            IWindowManager windowManager, TorrentService torrentService)
        {
            _eventAggregator = eventAggregator;
            _logger = logger;
            _windowManager = windowManager;
            _torrentService = torrentService;
            DisplayName = "History";
        }

        protected override async void OnInitialActivate()
        {
            base.OnInitialActivate();
            await LoadEpisodes();
        }


        public async Task LoadEpisodes()
        {
            await using var db = new TrackContext();
            var episodes = await db.Episodes.Where(e => e.Hide)
                .ToListAsync();

            Items.Clear();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                episodes = episodes.Where(a => a.Name.Contains(_search, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }


            episodes = FilterStatus switch
            {
                Filter.Watched => episodes.Where(a => a.Watched).ToList(),
                Filter.NotWatched => episodes.Where(a => !a.Watched).ToList(),
                _ => episodes
            };

            Items.AddRange(episodes
                .OrderByDescending(e => e.HideDate)
                .ThenByDescending(e => e.Released));
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

        public async Task ToggleWatch(Episode anime)
        {
            if (anime == null)
            {
                return;
            }

            anime.WatchedDate = anime.Watched ? anime.WatchedDate : DateTime.Now;
            anime.Watched = !anime.Watched;
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
                anime.WatchedDate = DateTime.Now;
                await anime.UpdateInDatabase();
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed opening file to watch");
            }
        }


        public async Task DeleteWithFile()
        {
            var anime = (Episode)ActiveItem;
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

        public async Task UnDeleteItem(Episode episode)
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

        public void OpenExternalLink(Episode anime)
        {
            LinkUtil.Open(anime.Link);
        }

        public void OpenFolder(Episode anime)
        {
            _windowManager.ShowWindow(new FolderFilesViewModel(ref anime, _eventAggregator, _logger));
        }
    }
}
