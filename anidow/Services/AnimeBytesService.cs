using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.Model;
using Anidow.Utils;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesService : RssFeedService, INotifyPropertyChanged
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly TorrentService _torrentService;
        private int _initialRefreshTime;
        private Timer _tracker;

        public AnimeBytesService(ILogger logger, IEventAggregator eventAggregator, HttpClient httpClient,
            SettingsService settingsService, TorrentService torrentService, TaskbarIcon taskbarIcon)
            : base(logger, httpClient)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _httpClient = httpClient;
            _settingsService = settingsService;
            _torrentService = torrentService;
            _taskbarIcon = taskbarIcon;
        }

        private SettingsModel Settings => _settingsService.Settings;

        private string AllAnimeUrl =>
            $"https://animebytes.tv/feed/rss_torrents_anime/{Settings.AnimeBytesSettings.PassKey}";

        private string AiringAnimeUrl =>
            $"https://animebytes.tv/feed/rss_torrents_airing_anime/{Settings.AnimeBytesSettings.PassKey}";

        public bool TrackerIsRunning { get; set; }
        public DateTime LastCheck { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InitTracker()
        {
            _tracker = new Timer { Interval = 1000 * 60 * _settingsService.Settings.RefreshTime };
            _tracker.Elapsed += TrackerOnElapsed;
            _initialRefreshTime = _settingsService.Settings.RefreshTime;
            _settingsService.SettingsSavedEvent += OnSettingsSavedEvent;
            if (!string.IsNullOrWhiteSpace(_settingsService.Settings.AnimeBytesSettings.PassKey))
            {
                StartTracker();
            }
        }

        public void StartTracker()
        {
            LastCheck = DateTime.Now;
            _tracker.Start();
            TrackerIsRunning = true;
        }

        public void StopTracker()
        {
            _tracker.Stop();
            TrackerIsRunning = false;
        }

        private void OnSettingsSavedEvent(object sender, EventArgs e)
        {
            if (_settingsService.Settings.RefreshTime == _initialRefreshTime)
            {
                return;
            }

            StopTracker();

            _initialRefreshTime = _settingsService.Settings.RefreshTime;
            _tracker.Interval = 1000 * 60 * _initialRefreshTime;

            if (string.IsNullOrWhiteSpace(_settingsService.Settings.AnimeBytesSettings.PassKey))
            {
                return;
            }

            StartTracker();
        }

        private async void TrackerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await CheckForNewEpisodes();
        }

        public async Task CheckForNewEpisodes()
        {
            LastCheck = DateTime.Now;
            var animeBytesPassKey = _settingsService.Settings.AnimeBytesSettings.PassKey;
            if (string.IsNullOrWhiteSpace(animeBytesPassKey))
            {
                return;
            }

            await using var db = new TrackContext();
            var anime = await db.Anime
                .Where(a => a.Site == Site.AnimeBytes
                            && a.Status == AnimeStatus.Watching)
                .ToListAsync();

            var feedItems = await GetFeedItems(AnimeBytesFilter.Airing);
            feedItems.Reverse();
            foreach (var a in anime)
            {
                var episodes = await db.Episodes
                    .Where(e => e.AnimeId == a.GroupId)
                    .ToListAsync();
                foreach (var item in feedItems)
                {
                    var episode = episodes.FirstOrDefault(ep => ep.Name == item.Name);
                    if (episode != null)
                    {
                        continue;
                    }

                    if (item.GroupId != a.GroupId)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(item.GetEpisode()))
                    {
                        // item is probably a batch and we don't want to download it (no batches in airing feed)
                        // or something else idk
                        _logger.Information("item.GetEpisode() returned null or empty string", item);
                        _logger.Debug($"{JsonSerializer.Serialize(item)}");
                        continue;
                    }

                    if (item.GetReleaseGroup() != a.Group)
                    {
                        continue;
                    }

                    var parts = item.Name.Split('|', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToList();

                    if (!parts.Contains(a.Resolution) && !string.IsNullOrWhiteSpace(a.Resolution))
                    {
                        continue;
                    }

                    item.Folder = a.Folder;
                    var success = await _torrentService.Download(item);
                    if (success)
                    {
                        var newEpisode = new Episode
                        {
                            AnimeId = a.GroupId,
                            Name = item.Name,
                            Cover = a.Cover,
                            CoverData = a.CoverData,
                            Folder = a.Folder,
                            Released = item.Released,
                            DownloadLink = item.DownloadLink,
                            Link = item.GroupUrl,
                            Site = Site.AnimeBytes
                        };

                        await db.AddAsync(newEpisode);
                        await db.SaveChangesAsync();

                        _eventAggregator.PublishOnUIThread(new RefreshHomeEvent());

                        if (_settingsService.Settings.Notifications)
                        {
                            _taskbarIcon.ShowBalloonTip("Added", item.Name, BalloonIcon.None);
                        }

                        break;
                    }
                }
            }
        }

        public async Task<List<AnimeBytesTorrentItem>> GetFeedItems(AnimeBytesFilter filter)
        {
            if (string.IsNullOrWhiteSpace(Settings.AnimeBytesSettings.PassKey))
            {
                _logger.Information("animebytes passkey is empty");
                return default;
            }

            return filter switch
            {
                AnimeBytesFilter.All => await GetFeedItems(AllAnimeUrl, ToDomain) ?? new List<AnimeBytesTorrentItem>(),
                AnimeBytesFilter.Airing => await GetFeedItems(AiringAnimeUrl, ToDomain) ??
                                           new List<AnimeBytesTorrentItem>(),
                _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
            };
        }

        private AnimeBytesTorrentItem ToDomain(SyndicationItem item)
        {
            var name = GetElementExtensionValue(item, "groupTitle");
            Array.ForEach(Path.GetInvalidFileNameChars(),
                c => name = name.Replace(c.ToString(), string.Empty));

            var feedItem = new AnimeBytesTorrentItem
            {
                Released = item.PublishDate.DateTime,
                Name = item.Title.Text,
                DownloadLink = item.Links[0].Uri.AbsoluteUri,
                Link = item.Id,
                Description = HtmlUtil.ConvertToPlainText(item.Summary?.Text ?? string.Empty),
                Category = item.Categories.FirstOrDefault()?.Name,
                Folder = Path.Join(Settings.AnimeFolder, name),
                Size = GetElementExtensionValue(item, "size"),
                GroupId = GetElementExtensionValue(item, "groupId"),
                GroupTitle = GetElementExtensionValue(item, "groupTitle"),
                GroupUrl = GetElementExtensionValue(item, "groupUrl"),
                Cover = GetElementExtensionValue(item, "cover"),
                TorrentProperty = GetElementExtensionValue(item, "torrentProperty")
            };

            return feedItem;
        }

        public async Task<AnimeBytesScrapeResult> SearchAnime(string search)
        {
            var username = _settingsService.Settings.AnimeBytesSettings.Username;
            var passkey = _settingsService.Settings.AnimeBytesSettings.PassKey;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passkey))
            {
                return default;
            }

            var url =
                $"https://animebytes.tv/scrape.php?torrent_pass={passkey}&username={username}&type=anime&searchstr={search}";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var anime = JsonSerializer.Deserialize<AnimeBytesScrapeResult>(response);
                if (anime == null)
                {
                    _logger.Error("AnimeBytesScrapeResult is null");
                    return default;
                }

                var minSeeders = _settingsService.Settings.NyaaSettings.HideTorrentsBelowSeeders;

                for (var index = 0; index < anime.Groups.Length; index++)
                {
                    var a = anime.Groups[index];
                    a.Row = index;
                    DecodeProperties(a.GetType(), a);
                    a.SelectedTorrent = a.Torrents.FirstOrDefault();
                    foreach (var aTorrent in a.Torrents)
                    {
                        DecodeProperties(aTorrent.GetType(), aTorrent);
                        DecodeProperties(aTorrent.EditionData.GetType(), aTorrent.EditionData);
                        aTorrent.Folder = _settingsService.Settings.AnimeBytesSettings.DefaultDownloadFolder;
                    }

                    if (minSeeders > -1)
                    {
                        a.Torrents = a.Torrents.Where(i => i.Seeders >= minSeeders).ToArray();
                    }

                    var je = (JsonElement)a.Synonymns;
                    var json = je.GetRawText();

                    a.SynonymnsList = json switch
                    {
                        { } j when j.StartsWith("[") =>
                            JsonSerializer.Deserialize<List<string>>(json),
                        { } j when j.StartsWith("{") =>
                            JsonSerializer.Deserialize<Dictionary<string, string>>(json)?.Values.ToList(),
                        _ => new List<string>()
                    };

                    je = (JsonElement)a.Links;
                    json = je.GetRawText();

                    a.LinksDict = json switch
                    {
                        { } j when j.StartsWith("{") =>
                            JsonSerializer.Deserialize<Dictionary<string, string>>(json),
                        _ => new Dictionary<string, string>()
                    };
                }

                return anime;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed getting animebytes search result");
            }

            return default;
        }

        private void DecodeProperties(Type type, object obj)
        {
            var propertiesInfo = type.GetProperties();

            foreach (var p in propertiesInfo)
                if (p.PropertyType.FullName == "System.String" && p.CanWrite)
                {
                    var value = p.GetValue(obj, null);
                    p.SetValue(obj, HttpUtility.HtmlDecode(value as string), null);
                }
        }
    }
}