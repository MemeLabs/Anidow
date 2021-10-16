using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Services;
using Anidow.Utils;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace Anidow.Pages;

public static class HistoryFilterType
{
    public const string All = "All";
    public const string Watched = "Watched";
    public const string NotWatched = "Not watched";
}

public class HistoryViewModel : Conductor<Episode>.Collection.OneActive
{
    private const int MaxFilesInView = 50;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger _logger;
    private readonly TorrentService _torrentService;
    private readonly IWindowManager _windowManager;
    private CancellationTokenSource _cancellationTokenSource = new();
    private string _filterStatus = HistoryFilterType.All;

    private ScrollViewer _scrollViewer;

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
        { HistoryFilterType.All, HistoryFilterType.Watched, HistoryFilterType.NotWatched };

    public string FilterStatus
    {
        get => _filterStatus;
        set
        {
            SetAndNotify(ref _filterStatus, value);
            Execute.OnUIThreadAsync(async () => await HomePage());
        }
    }

    public string Search { get; set; }

    public string EpisodesLoaded { get; set; }

    public bool CanLoadMore { get; set; } = true;
    public bool CanLoadEpisodes { get; set; } = true;

    protected override async void OnInitialActivate()
    {
        await HomePage();
    }

    public async Task RefreshPage()
    {
        await LoadPage(Page);
    }

    public async Task HomePage()
    {
        if (!CanLoadEpisodes)
        {
            return;
        }
        
        var page = Page;
        Page = 1;

        await LoadPage(page);
    }

    public bool CanNextPage => Page < TotalPages;
    public async Task NextPage()
    {
        if (Page == TotalPages || !CanLoadEpisodes)
        {
            return;
        }

        var page = Page;
        Page += 1;

        await LoadPage(page);
    }

    public bool CanPreviousPage => Page > 1;
    public async Task PreviousPage()
    {
        if (Page == 1 || !CanLoadEpisodes)
        {
            return;
        }

        var page = Page;
        Page -= 1;

        await LoadPage(page);
    }

    protected void ResetCancellationTokenSource()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }
    private async Task LoadPage(int page)
    {
        if (!CanLoadEpisodes)
        {
            ResetCancellationTokenSource();
            return;
        }

        //await Execute.OnUIThreadAsync(async () =>
        //{
        try
        {
            var token = _cancellationTokenSource.Token;
            var success = await LoadEpisodes(token);
            if (!success)
            {
                Page = page;
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
            CanLoadEpisodes = true;
        }
        //});
    }


    public async Task<bool> LoadEpisodes(CancellationToken cancellationToken)
    {
        CanLoadEpisodes = false;
        await using var db = new TrackContext();

        var episodes = await db.Episodes
                               .Where(e => e.Hide)
                               .ToListAsync(cancellationToken);


        if (!string.IsNullOrWhiteSpace(Search))
        {
            episodes = episodes.Where(a => a.Name.Contains(Search.Trim(), StringComparison.InvariantCultureIgnoreCase))
                               .ToList();
        }

        episodes = FilterStatus switch
        {
            HistoryFilterType.Watched => episodes.Where(a => a.Watched).ToList(),
            HistoryFilterType.NotWatched => episodes.Where(a => !a.Watched).ToList(),
            _ => episodes,
        };


        if (episodes.Count <= 0)
        {
            CanLoadEpisodes = true;
            return false;
        }

        TotalPages = (int)Math.Ceiling(episodes.Count / (double)MaxFilesInView);

        episodes = episodes
                   .OrderByDescending(e => e.HideDate)
                   .ThenByDescending(e => e.Released)
                   .Skip((Page - 1) * MaxFilesInView)
                   .Take(MaxFilesInView)
                   .ToList();


        Items.Clear();

        _scrollViewer?.ScrollToTop();

        foreach (var episode in episodes.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            await DispatcherUtil.DispatchAsync(() => Items.Add(episode));

        ActiveItem = null;
//#if DEBUG
//        Items.Add(new Episode
//        {
//            Name = "test :: Episode 1",
//            Released = DateTime.Today,
//            Folder = Directory.GetCurrentDirectory(),
//        });
//#endif

        CanLoadEpisodes = true;
        return true;
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
                _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
                return;
            }

            await ProcessUtil.OpenFile(episode);

            episode.Watched = true;
            episode.WatchedDate = DateTime.UtcNow;
            await episode.UpdateInDatabase();
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed opening file");
            MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                icon: MessageBoxImage.Error);
            await OpenFolder(episode);
        }
    }

    public async Task DeleteItem(Episode episode)
    {
        episode ??= ActiveItem;
        var index = Items.IndexOf(episode);
        if (index == -1)
        {
            return;
        }

        if (!DeleteUtil.AskForConfirmation(episode.Name))
        {
            return;
        }

        try
        {
            if (await episode.DeleteInDatabase())
            {
                Items.Remove(episode);
                DeselectItem();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed deleting episode in database");
        }
    }


    public async Task DeleteWithFile(Episode episode)
    {
        episode ??= ActiveItem;

        if (!DeleteUtil.AskForConfirmation(episode.Name))
        {
            return;
        }

        var success = await _torrentService.Remove(episode, true);

        // wait 1 second for the torrent client to delete the file
        await Task.Delay(1.Seconds());

        if (!success)
        {
            return;
        }

        if (await episode.DeleteInDatabase())
        {
            Items.Remove(episode);
            DeselectItem();
        }

        if (!string.IsNullOrWhiteSpace(episode.File) && File.Exists(episode.File))
        {
            try
            {
                File.Delete(episode.File);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"failed deleting file {episode.File}");
            }
        }
    }

    public async Task UnHideItem(Episode episode)
    {
        episode ??= ActiveItem;
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
        _eventAggregator.PublishOnUIThread(new RefreshHomeEvent());
    }

    public void OpenExternalLink(Episode episode)
    {
        LinkUtil.Open(episode.Link);
    }

    public async Task OpenFolder(Episode episode)
    {
        episode.CanOpen = false;
        _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
        await Task.Delay(100);
        episode.CanOpen = true;
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

    public void OnPreviewKeyDown(object _, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        HomePage();
        e.Handled = true;
    }

    #region Paging

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }

    #endregion
}