using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Anidow.Database.Models;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Utils;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace Anidow.Pages
{
    public class FolderFilesViewModel : Screen
    {
        public string Folder { get; set; }
        private readonly Episode _episode;
        private readonly Anime _anime;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        public string _name;

        private List<FolderFilesModel> _files;
        private readonly int _maxFilesInView = 25;

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

        public BindableCollection<FolderFilesModel> FileInfos { get; }

        protected override void OnActivate()
        {
            GetFilesFromFolder();
        }

        public bool CanGetFilesFromFolder { get; set; } = true;
        public void GetFilesFromFolder(bool clear = false)
        {
            if (!Directory.Exists(Folder))
            {
                return;
            }
            CanGetFilesFromFolder = false;
            var files = Directory.GetFiles(Folder)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime);
            
            _files = new List<FolderFilesModel>();
            foreach (var file in files)
            {
                var item = new FolderFilesModel { File = file };
                if (_episode != null)
                {
                    var nameSplit = file.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim());
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

            LoadMore();
#if DEBUG
            if (FileInfos.Count >= 4)
            {
                FileInfos[3].Highlight = true;
            }
#endif
            CanGetFilesFromFolder = true;
        }

        public bool CanLoadMore { get; set; }
        public void LoadMore()
        {
            FileInfos.AddRange(_files.Skip(FileInfos.Count).Take(_maxFilesInView));
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
            RequestClose(null);
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

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            GetFilesFromFolder();
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
