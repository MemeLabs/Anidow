using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Interfaces;
using Anidow.Model;
using Anidow.Pages.Components.Notify;
using Anidow.Services;
using Anidow.Utils;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Stylet;
using MessageBox = AdonisUI.Controls.MessageBox;
using Screen = Stylet.Screen;

namespace Anidow.Pages
{
    public class NotifyViewModel : Screen, IHandle<NotifyItemAddOrUpdateEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly NotifyService _notifyService;
        private readonly TorrentService _torrentService;
        private readonly IWindowManager _windowManager;
        private NotifySite _siteFilter = NotifySite.All;

        public NotifyViewModel(IWindowManager windowManager,
            IEventAggregator eventAggregator,
            NotifyService notifyService,
            TorrentService torrentService,
            ILogger logger)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _eventAggregator = eventAggregator;
            _notifyService = notifyService;
            _torrentService = torrentService;
            _logger = logger;
            Items = new BindableCollection<NotifyItem>();
            DisplayName = "Notify";

            //JobManager.AddJob(
            //    () => { BadgeContent = CountUnseenMatches(); },
            //    s => s.WithName("Notify:CountUnseenMatches")
            //          .NonReentrant()
            //          .ToRunEvery(1)
            //          .Seconds()
            //);
            eventAggregator.Subscribe(this);
        }

        public BindableCollection<NotifyItem> Items { get; set; }

        public NotifySite SiteFilter
        {
            get => _siteFilter;
            set
            {
                SetAndNotify(ref _siteFilter, value);
                Debouncer.DebounceAction("Notify:SiteFiler:LoadItems", async _ => { await LoadItems(); },
                    50.Milliseconds());
            }
        }

        public string BadgeContent => CountUnseenMatches();

        public void Handle(NotifyItemAddOrUpdateEvent message)
        {
            if (message.IsUpdate)
            {
                var item = Items.FirstOrDefault(i => i.Id == message.Item.Id);
                if (item is not null)
                {
                    foreach (var nm in message.Item.Matches)
                    {
                        if (item.Matches.Any(m => m.Id == nm.Id ||
                                                  nm.DownloadLink == m.DownloadLink))
                        {
                            continue;
                        }

                        DispatcherUtil.DispatchSync(() => { item.Matches.Add(nm); });
                    }
                }
            }
            else
            {
                DispatcherUtil.DispatchSync(() => Items.Insert(0, message.Item));
            }

            UpdateBadgeContent();
        }

        private string CountUnseenMatches()
        {
            var matches = Items.Sum(item => item.MatchesUnseen);
            return matches switch
            {
                > 99 => "99+",
                > 0 and < 100 => $"{matches}",
                _ => null,
            };
        }

        public async Task Init()
        {
            await LoadItems();
            _notifyService.Init();
        }

        private async Task LoadItems()
        {
            Items.Clear();
            await using var db = new TrackContext();
            var items = await db.NotifyItems
                                .Include(n => n.Matches)
                                .Include(m => m.Keywords)
                                .Where(i => SiteFilter.HasFlag(i.Site))
                                .OrderByDescending(i => i.Created)
                                .ToListAsync();
            foreach (var item in items) await DispatcherUtil.DispatchAsync(() => Items.Add(item));

            UpdateBadgeContent();
        }

        public void Add()
        {
            _windowManager.ShowDialog(new NotifyAddViewModel(_eventAggregator));
        }

        public void Edit(NotifyItem item)
        {
            var result = _windowManager.ShowDialog(new NotifyAddViewModel(item, _eventAggregator));
            if (result is true)
            {
                item.OnPropertyChanged("KeywordsString");
                NotifyOfPropertyChange(nameof(item));
            }
        }

        public async Task ClearMatches(NotifyItem item)
        {
            await using var db = new TrackContext();
            db.Attach(item);
            item.Matches.Clear();
            db.Update(item);
            await db.SaveChangesAsync();
            UpdateBadgeContent();
        }

        public async Task Delete(NotifyItem item)
        {
            var result = MessageBox.Show("are you sure?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Items.Remove(item);
                await using var db = new TrackContext();
                db.Attach(item);
                db.Remove(item);
                await db.SaveChangesAsync();
            }

            UpdateBadgeContent();
        }

        private void UpdateBadgeContent()
        {
            NotifyOfPropertyChange(nameof(BadgeContent));
            foreach (var notifyItem in Items) notifyItem.OnPropertyChanged(nameof(notifyItem.MatchesUnseen));
        }


        public async Task MarkAsSeen(NotifyItemMatch item)
        {
            item.Seen = true;
            UpdateBadgeContent();


            await using var db = new TrackContext();
            db.Attach(item);
            db.Update(item);
            await db.SaveChangesAsync();
        }

        public void OpenLink(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return;
            }

            LinkUtil.Open(link);
        }

        public async Task Download(NotifyItemMatch match)
        {
            switch (match.Site)
            {
                case NotifySite.AnimeBytes:
                    await DownloadFile<AnimeBytesTorrentItem>(match);
                    break;
                case NotifySite.Nyaa:
                    await DownloadFile<NyaaTorrentItem>(match);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string OpenFolderBrowserDialog()
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Directory.GetCurrentDirectory(),
            };
            var result = dialog.ShowDialog();
            return result == DialogResult.OK ? dialog.SelectedPath : default;
        }

        private async Task DownloadFile<T>(NotifyItemMatch match) where T : ITorrentItem
        {
            if (string.IsNullOrEmpty(match.Json))
            {
                return;
            }

            try
            {
                var ab = JsonConvert.DeserializeObject<T>(match.Json);
                var folder = ab.Folder = OpenFolderBrowserDialog();
                if (folder is null)
                {
                    return;
                }

                var (success, torrent) = await _torrentService.Download(ab);
                if (success)
                {
                    _eventAggregator.PublishOnUIThread(new DownloadEvent
                    {
                        Torrent = torrent,
                        Item = ab,
                    });
                    match.Downloaded = true;
                    await using var db = new TrackContext();
                    db.Attach(match);
                    db.Update(match);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed downloading");
            }
        }


        public async Task CheckNow(NotifyItem item)
        {
            item.CanCheckNow = false;
            switch (item.Site)
            {
                case NotifySite.All:
                    await _notifyService.NyaaJob(item.Id, null);
                    await _notifyService.AnimeBytesJob(item.Id, null);
                    break;
                case NotifySite.AnimeBytes:
                    await _notifyService.AnimeBytesJob(item.Id, null);
                    break;
                case NotifySite.Nyaa:
                    await _notifyService.NyaaJob(item.Id, null);
                    break;
            }

            await Task.Delay(100);
            item.CanCheckNow = true;
        }
    }

    public class NotifyItemAddOrUpdateEvent
    {
        public NotifyItem Item { get; init; }
        public bool IsUpdate { get; set; }
    }

    public class NotifyItem : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public string Name { get; set; }
        public NotifySite Site { get; set; } = NotifySite.All;
        public bool MatchAll { get; set; }
        public ICollection<NotifyItemKeyword> Keywords { get; set; } = new BindableCollection<NotifyItemKeyword>();
        public ICollection<NotifyItemMatch> Matches { get; set; } = new BindableCollection<NotifyItemMatch>();

        [NotMapped]
        public string KeywordsString =>
            Keywords is not null ? string.Join(", ", Keywords?.Select(k => k.Word)) : string.Empty;

        [NotMapped] public bool Matched => Matches?.Count > 0;
        [NotMapped] public int MatchesUnseen => Matches?.Count(m => !m.Seen) ?? 0;
        [NotMapped] public bool CanCheckNow { get; set; } = true;
    }

    public class NotifyItemKeyword
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;

        public string Word { get; set; }
        public bool IsRegex { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool MustMatch { get; set; }

        public int NotifyItemId { get; set; }
        public virtual NotifyItem NotifyItem { get; set; }
    }

    public class NotifyItemMatch : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public NotifySite Site { get; set; }
        public bool Seen { get; set; }
        public string Name { get; set; }
        public string DownloadLink { get; set; }
        public string Link { get; set; }
        public string Json { get; set; }
        public bool UserNotified { get; set; }
        public bool Downloaded { get; set; }
        public string KeywordsData { get; set; }

        [NotMapped]
        public string[] Keywords
        {
            get => KeywordsData?.Split("\0");
            set => KeywordsData = string.Join("\0", value);
        }

        public int NotifyItemId { get; set; }
        public virtual NotifyItem NotifyItem { get; set; }
    }
}