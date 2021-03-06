using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using Serilog;
using Stylet;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListView = System.Windows.Controls.ListView;

namespace Anidow.Pages;

// ReSharper disable once ClassNeverInstantiated.Global
public class NyaaViewModel : Conductor<NyaaTorrentItem>.Collection.OneActive
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger _logger;
    private readonly NyaaService _nyaaService;
    private readonly TorrentService _torrentService;
    private ScrollViewer _scrollViewer;

    public NyaaViewModel(ILogger logger, IEventAggregator eventAggregator, NyaaService nyaaService,
        TorrentService torrentService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _nyaaService = nyaaService ?? throw new ArgumentNullException(nameof(nyaaService));
        _torrentService = torrentService ?? throw new ArgumentNullException(nameof(torrentService));
        DisplayName = "Nyaa";
    }

    public string SearchText { get; set; } = string.Empty;
    public string LastSearch { get; set; }

    public List<string> Filters => new()
    {
        "No filter",
        "No remakes",
        "Trusted only",
    };

    public int SelectedFilterIndex { get; set; }

    public bool CanGetItems { get; set; }

    //public bool CanDownload => ActiveItem != null && !string.IsNullOrWhiteSpace(ActiveItem.Folder);

    public async Task GetItems()
    {
        CanGetItems = false;
        var items = await _nyaaService.GetFeedItems((NyaaFilter)SelectedFilterIndex, SearchText.Trim());

        if (items is not { Count: > 0 })
        {
            CanGetItems = true;
            return;
        }

        Items.Clear();
        foreach (var item in items) await DispatcherUtil.DispatchAsync(() => Items.Add(item));

        _scrollViewer?.ScrollToTop();
        ActiveItem = null!;
        LastSearch = $"{DateTime.Now:T}";
        CanGetItems = true;
    }

    public void DeselectItem()
    {
        ChangeActiveItem(null, false);
    }

    public void OpenExternalLink(NyaaTorrentItem item)
    {
        LinkUtil.Open(item.Link);
    }

    public async Task Download(NyaaTorrentItem item)
    {
        item.CanDownload = false;
        _logger.Information($"downloading {item.Name}");
        var (success, torrent) = await _torrentService.Download(item);
        if (success)
        {
            _eventAggregator.PublishOnUIThread(new DownloadEvent
            {
                Item = item,
                Torrent = torrent,
            });
        }

        await Task.Delay(100);
        item.CanDownload = true;
    }

    protected override void OnInitialActivate()
    {
        _ = GetItems();
    }

    public async Task OnPreviewKeyDown(object _, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        await GetItems().ConfigureAwait(false);
        e.Handled = true;
    }

    public void OpenFolderBrowserDialog()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = ActiveItem.Folder,
        };
        var result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
            ActiveItem.Folder = dialog.SelectedPath;
        }
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
}