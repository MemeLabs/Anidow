using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Extensions;
using Anidow.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace Anidow.Pages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TrackedViewModel : Conductor<Anime>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;
        private readonly ILogger _logger;
        private string _search;

        public AnimeStatus FilterStatus { get; set; } = AnimeStatus.Watching;

        public string Search
        {
            get => _search;
            set
            {
                SetAndNotify(ref this._search, value);
                Debouncer.DebounceAction("load_tracked", async config =>
                {
                    await Load();
                });
            }
        }

        public TrackedViewModel(IEventAggregator eventAggregator, IWindowManager windowManager, ILogger logger)
        {
            _eventAggregator = eventAggregator;
            _windowManager = windowManager;
            _logger = logger;
            DisplayName = "Tracked";
        }

        protected override async void OnInitialActivate()
        {
            await Load();
        }

        public bool CanLoad { get; set; }

        public async Task Load()
        {
            CanLoad = false;
            await using var db = new TrackContext();
            var anime = await Task.Run(async () => await db.Anime
                .OrderByDescending(a => a.Released)
                .ToListAsync());

            if (!string.IsNullOrWhiteSpace(Search))
            {
                anime = anime.Where(a => a.Name.Contains(_search, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            anime = FilterStatus switch
            {
                AnimeStatus.Watching => anime.Where(a => a.Status == AnimeStatus.Watching).ToList(),
                AnimeStatus.Finished => anime.Where(a => a.Status == AnimeStatus.Finished).ToList(),
                _ => anime
            };

            Items.Clear();
            foreach (var a in anime)
            {
                a.Episodes = await db.Episodes.CountAsync(e => e.AnimeId == a.GroupId);
                Execute.OnUIThreadSync(() => Items.Add(a));
                await Task.Delay(1);
            }

            CanLoad = true;
        }

        public async Task Delete(Anime anime)
        {
            var result = MessageBox.Show($"delete?\n\n{anime.Name}", "Delete",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            await using var db = new TrackContext();
            db.Attach(anime);
            db.Remove(anime);
            await db.SaveChangesAsync();
            Items.Remove(anime);
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
        }


        public void OpenFolderBrowserDialog(Anime anime)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = anime.Folder
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
    }
}