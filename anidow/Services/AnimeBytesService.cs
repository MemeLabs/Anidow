using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stylet;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AnimeBytesService : RssFeedService, INotifyPropertyChanged
    {
        private readonly ILogger _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;
        private readonly TorrentService _torrentService;
        private readonly TaskbarIcon _taskbarIcon;
        private Timer _tracker;
        private int _initialRefreshTime;

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
        
        private SettingsModel Settings => _settingsService.GetSettings();

        private string AllAnimeUrl => $"https://animebytes.tv/feed/rss_torrents_anime/{Settings.AnimeBytesSettings.PassKey}";
        private string AiringAnimeUrl => $"https://animebytes.tv/feed/rss_torrents_airing_anime/{Settings.AnimeBytesSettings.PassKey}";

        public bool TrackerIsRunning { get; set; }
        public DateTime LastCheck { get; private set; }
        public void InitTracker()
        {
            _tracker = new Timer {Interval = 1000 * 60 * _settingsService.GetSettings().RefreshTime};
            _tracker.Elapsed += TrackerOnElapsed;
            StartTracker();
            _initialRefreshTime = _settingsService.GetSettings().RefreshTime;
            _settingsService.SettingsSaved += SettingsServiceOnSettingsSaved;
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

        private void SettingsServiceOnSettingsSaved(object sender, EventArgs e)
        {
            if (_settingsService.GetSettings().RefreshTime == _initialRefreshTime)
            {
                return;
            }

            _tracker.Stop();
            _tracker.Interval = 1000 * 60 * _settingsService.GetSettings().RefreshTime;
            StartTracker();
        }

        private async void TrackerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await CheckForNewEpisodes();
        }

        public async Task CheckForNewEpisodes()
        {
            LastCheck = DateTime.Now;
            var animeBytesPassKey = _settingsService.GetSettings().AnimeBytesSettings.PassKey;
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
                        _logger.Information("item.GetEpisode() returned null or empty string");
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
                            Folder = a.Folder,
                            Released = item.Released,
                            DownloadLink = item.DownloadLink,
                            Link = item.GroupUrl
                        };
                        await db.AddAsync(newEpisode);
                        await db.SaveChangesAsync();
                        _eventAggregator.PublishOnUIThread(new RefreshHomeEvent());

                        if (_settingsService.GetSettings().Notifications)
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
                AnimeBytesFilter.Airing => await GetFeedItems(AiringAnimeUrl, ToDomain) ?? new List<AnimeBytesTorrentItem>(),
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
            var username = _settingsService.GetSettings().AnimeBytesSettings.Username;
            var passkey = _settingsService.GetSettings().AnimeBytesSettings.PassKey;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passkey))
            {
                return default;
            }
            var url = $"https://animebytes.tv/scrape.php?torrent_pass={passkey}&username={username}&type=anime&searchstr={search}";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var anime = JsonSerializer.Deserialize<AnimeBytesScrapeResult>(response);
                
                var minSeeders = _settingsService.GetSettings().NyaaSettings.HideTorrentsBelowSeeders;

                foreach (var a in anime.Groups)
                {
                    DecodeProperties(a.GetType(), a);
                    a.SelectedTorrent = a.Torrents.FirstOrDefault();
                    foreach (var aTorrent in a.Torrents)
                    {
                        DecodeProperties(aTorrent.GetType(), aTorrent);
                        DecodeProperties(aTorrent.EditionData.GetType(), aTorrent.EditionData);
                        aTorrent.Folder = _settingsService?.GetSettings().AnimeBytesSettings.DefaultDownloadFolder;
                    }

                    if (minSeeders > -1)
                    {
                        a.Torrents = a.Torrents.Where(i => i.Seeders >= minSeeders).ToArray();
                    }
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
            {
                if (p.PropertyType.FullName == "System.String" && p.CanWrite)
                {
                    var value = p.GetValue(obj, null);
                    p.SetValue(obj, HttpUtility.HtmlDecode(value as string), null);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}