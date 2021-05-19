using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AdonisUI.Controls;
using Anidow.Database.Models;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Utils;
using Serilog;
using Stylet;

namespace Anidow.Pages
{
    public class FolderFilesViewModel : Screen
    {
        private readonly Anime _anime;
        private readonly Episode _episode;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly int _maxFilesInView = 25;
        private readonly string _name;

        private List<FolderFilesModel> _files;

        public FolderFilesViewModel(ref Episode episode, IEventAggregator eventAggregator, ILogger logger)
        {
            _episode = episode;
            _name = episode.Name;
            Folder = episode.Folder;
            _eventAggregator = eventAggregator;
            _logger = logger;
            FileInfos = new BindableCollection<FolderFilesModel>();
            DisplayName = $"Files - {episode.Name}";
        }

        public FolderFilesViewModel(ref Anime anime, IEventAggregator eventAggregator, ILogger logger)
        {
            _anime = anime;
            _name = anime.Name;
            Folder = anime.Folder;
            _eventAggregator = eventAggregator;
            _logger = logger;
            FileInfos = new BindableCollection<FolderFilesModel>();
            DisplayName = $"Files - {anime.Name}";
        }

        public string Folder { get; set; }

        public BindableCollection<FolderFilesModel> FileInfos { get; }

        public bool CanGetFilesFromFolder { get; set; } = true;

        public bool CanLoadMore { get; set; }

        protected override void OnActivate()
        {
            _ = GetFilesFromFolder();
        }

        public async Task GetFilesFromFolder(bool clear = false)
        {
            if (!Directory.Exists(Folder))
            {
                return;
            }

            CanGetFilesFromFolder = false;
            var files = await Task.Run(() => Directory.GetFiles(Folder)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime));

            _files = new List<FolderFilesModel>();
            foreach (var file in files)
            {
                var item = new FolderFilesModel {File = file};
                if (_episode != null)
                {
                    var nameSplit = file.Name
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(f => f.Trim());
                    if (!string.IsNullOrEmpty(_episode.EpisodeNum) && nameSplit.Contains(_episode.EpisodeNum))
                    {
                        item.Highlight = true;
                    }
                }

                _files.Add(item);
            }

            if (clear)
            {
                FileInfos.Clear();
            }

            await LoadMore();
#if DEBUG
            if (FileInfos.Count >= 4)
            {
                FileInfos[3].Highlight = true;
            }
#endif
            CanGetFilesFromFolder = true;
        }

        public async Task LoadMore()
        {
            await Execute.OnUIThreadAsync(() => FileInfos.AddRange(_files.Skip(FileInfos.Count).Take(_maxFilesInView)));
            CanLoadMore = FileInfos.Count < _files.Count;
            DisplayName = $"Files ({FileInfos.Count}/{_files.Count}) - {_name}";
        }

        public async Task Watch(FileInfo fileInfo)
        {
            if (_episode != null)
            {
                _episode.File = fileInfo.FullName;
                _episode.Watched = true;
                _episode.WatchedDate = DateTime.UtcNow;
                try
                {
                    await _episode.UpdateInDatabase();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "failed updating episode in database");
                }
                //_eventAggregator.PublishOnUIThread(new RefreshHomeEvent());
            }

            try
            {
                ProcessUtil.OpenFile(fileInfo.FullName);
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed opening file");
            }
            finally
            {
                Close();
            }
        }

        public void Close()
        {
            RequestClose();
        }

        public void DeleteFile(FolderFilesModel file)
        {
            try
            {
                file.File.Delete();
                FileInfos.Remove(file);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error deleting", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            await GetFilesFromFolder();
            e.Handled = true;
        }

        public void OpenFolderInExplorer()
        {
            try
            {
                ProcessUtil.OpenFolder(Folder);
            }
            catch (Exception e)
            {
                _logger.Error(e, "couldn't open folder");
            }
        }
        // TODO figure out how highlited items are on Top
        //public void ListView_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var lv = (ListView)sender;
        //    var view = (CollectionView)CollectionViewSource.GetDefaultView(lv.ItemsSource);
        //    var groupDescription = new PropertyGroupDescription("Highlight");
        //    view.GroupDescriptions?.Add(groupDescription);
        //}
    }
}