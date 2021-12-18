using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AdonisUI.Controls;
using Anidow.Database.Models;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Utils;
using Serilog;
using Stylet;

namespace Anidow.Pages;

public class FolderFilesViewModel : Screen
{
    private const int MaxFilesInView = 100;
    private readonly Anime _anime;
    private readonly Episode _episode;
    private readonly ILogger _logger;
    private readonly string _name;
    private CancellationTokenSource _cancellationTokenSource = new();
    private string _filter = string.Empty;

    public FolderFilesViewModel(ref Episode episode, ILogger logger)
    {
        _episode = episode ?? throw new ArgumentNullException(nameof(episode));
        _name = episode.Name;
        Folder = episode.Folder;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        FileInfos = new BindableCollection<FolderFilesModel>();
        DisplayName = $"Files - {episode.Name}";
    }

    public FolderFilesViewModel(Anime anime, ILogger logger)
    {
        _anime = anime ?? throw new ArgumentNullException(nameof(anime));
        _name = anime.Name;
        Folder = anime.Folder;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        FileInfos = new BindableCollection<FolderFilesModel>();
        DisplayName = $"Files - {anime.Name}";
    }

    public BindableCollection<FolderFilesModel> FileInfos { get; }

    public string Folder { get; set; }
    public bool HasParentFolder => !string.IsNullOrWhiteSpace(ParentFolder);
    private string ParentFolder { get; set; }

    public bool CanLoadMore { get; set; }
    public bool Loading { get; set; }

    public string Filter
    {
        get => _filter;
        set
        {
            SetAndNotify(ref _filter, value);
            Debouncer.DebounceAction("folderView:filter", c =>
            {
                HighlightFoundItems(value);
                return Task.CompletedTask;
            }, TimeSpan.FromMilliseconds(100));
        }
    }

    private void HighlightFoundItems(string value)
    {
        try
        {
            foreach (var item in FileInfos)
                item.ShowInList = item.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase);
        }
        catch (Exception)
        {
            // ignore
        }
    }

    protected override void OnActivate()
    {
        _ = OpenFolder(Folder);
    }

    private void ResetCancellationTokenSource()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task GoUpFolder()
    {
        await OpenFolder(ParentFolder);
    }

    public async Task GetFilesFromFolder(bool clear = false)
    {
        if (!Directory.Exists(Folder))
        {
            return;
        }

        var files = await Task.Run(() => Directory.GetFileSystemEntries(Folder)
                                                  .Select(f => new FileInfo(f))
                                                  .OrderByDescending(f =>
                                                      f.Attributes.HasFlag(FileAttributes.Directory))
                                                  .ThenByDescending(f => f.LastWriteTime));

        if (clear)
        {
            FileInfos.Clear();
        }


        foreach (var file in files.TakeWhile(_ => !_cancellationTokenSource.IsCancellationRequested))
        {
            var item = new FolderFilesModel(file);
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

            item.ShowInList = item.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase);
            FileInfos.Add(item);
            await Task.Delay(1, _cancellationTokenSource.Token);
        }


#if DEBUG
        if (FileInfos.Count >= 4)
        {
            FileInfos[3].Highlight = true;
        }
#endif
    }

    public async Task Watch(FolderFilesModel file)
    {
        if (_episode != null)
        {
            if (string.IsNullOrWhiteSpace(_episode.File))
            {
                _episode.File = file.Path;
            }

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
            ProcessUtil.OpenFile(file.Path);
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed opening file");
            MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                icon: MessageBoxImage.Error);
            return;
        }

        Close();
    }

    public async Task OpenFolder(string path)
    {
        if (!Loading)
        {
            ResetCancellationTokenSource();
            await Task.Delay(50);
        }

        Loading = false;
        try
        {
            Folder = path;
            ParentFolder = Directory.GetParent(Folder)?.FullName;
            await GetFilesFromFolder(true);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            _logger.Error(e, "error opening folder");
        }

        Loading = true;
    }


    public void Close()
    {
        RequestClose();
    }

    public void DeleteFile(FolderFilesModel file)
    {
        try
        {
            File.Delete(file.Path);
            FileInfos.Remove(file);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "error deleting", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task OnPreviewKeyDown(object sender, KeyEventArgs e)
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

    public async Task SetFolder(FolderFilesModel file)
    {
        if (_episode is not null)
        {
            _episode.Folder = file.Path;
            await _episode.UpdateInDatabase();
        }

        if (_anime is not null)
        {
            _anime.Folder = file.Path;
            await _episode.UpdateInDatabase();
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