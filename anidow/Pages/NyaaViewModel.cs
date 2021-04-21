using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Services;
using Anidow.Utils;
using Serilog;
using Stylet;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Forms.ListBox;
using ListView = System.Windows.Controls.ListView;

namespace Anidow.Pages
{
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
            _logger = logger;
            _eventAggregator = eventAggregator;
            _nyaaService = nyaaService;
            _torrentService = torrentService;
            DisplayName = "Nyaa";
        }

        public string SearchText { get; set; } = string.Empty;
        public string LastSearch { get; set; }

        public List<string> Filters => new List<string>
        {
            "No filter",
            "No remakes",
            "Trusted only"
        };

        public int SelectedFilterIndex { get; set; }

        public bool CanGetItems { get; set; }

        //public bool CanDownload => ActiveItem != null && !string.IsNullOrWhiteSpace(ActiveItem.Folder);

        public async Task GetItems()
        {
            CanGetItems = false;
            var items = await _nyaaService.GetFeedItems(
                $"https://nyaa.si/?page=rss&c=1_2&f={SelectedFilterIndex}&q={SearchText}");

            if (items == default || items.Count <= 0)
            {
                return;
            }

            _orderType = "DESC";
            Items.Clear();
            Items.AddRange(items);

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
            _logger.Information($"downloading {item.Name}");
            var success = await _torrentService.Download(item);
            if (!success)
            {
                return;
            }

            _eventAggregator.PublishOnUIThread(new DownloadEvent
            {
                Item = item
            });
        }

        protected override async void OnInitialActivate()
        {
            await GetItems();
        }

        public async void OnPreviewKeyDown(object sender, KeyEventArgs e)
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
                SelectedPath = ActiveItem.Folder
            };
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                ActiveItem.Folder = dialog.SelectedPath;
            }
        }

        private string _orderType = "DESC";
        private string _orderColumn = "Added";

        public void Sort(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header)
            {
                return;
            }

            var columnName = header.Column.Header.ToString();

            if (columnName != _orderColumn)
            {
                _orderType = "DESC";
            }

            var orderedItems = columnName switch
            {
                "Added" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i => i.Released).ToList(),
                    "ASC" => Items.OrderByDescending(i => i.Released).ToList(),
                    _ => Items.OrderByDescending(i => i.Released).ToList()
                },
                "Name" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i => i.Name).ToList(),
                    "ASC" => Items.OrderByDescending(i => i.Name).ToList(),
                    _ => Items.OrderByDescending(i => i.Name).ToList()
                },
                "Seeders" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Seeders) ? "0" : i.Seeders)).ToList(),
                    "ASC" => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Seeders) ? "0" : i.Seeders)).ToList(),
                    _ => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Seeders) ? "0" : i.Seeders)).ToList(),
                },
                "Leechers" => _orderType switch
                {
                    "DESC" => Items.OrderBy(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Leechers) ? "0" : i.Leechers)).ToList(),
                    "ASC" => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Leechers) ? "0" : i.Leechers)).ToList(),
                    _ => Items.OrderByDescending(i =>
                        int.Parse(string.IsNullOrWhiteSpace(i.Leechers) ? "0" : i.Leechers)).ToList(),
                },
                _ => Items.ToList(),
            };

            _orderColumn = columnName;
            _orderType = _orderType switch
            {
                "DESC" => "ASC",
                "ASC" => "DESC",
                _ => "DESC"
            };

            Items.Clear();
            Items.AddRange(orderedItems);
            _scrollViewer?.ScrollToTop();
            e.Handled = true;
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
}