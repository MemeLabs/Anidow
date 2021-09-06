using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Events;
using Anidow.Model;
using Anidow.Pages;
using FluentScheduler;
using Hardcodet.Wpf.TaskbarNotification;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Stylet;

#nullable enable

namespace Anidow.Services
{
    public class NotifyService
    {
        private readonly AnimeBytesService _animeBytesService;
        private readonly IEventAggregator _eventAggregator;
        private readonly FeedStorageService _feedStorageService;
        private readonly ILogger _logger;
        private readonly NyaaService _nyaaService;
        private readonly SettingsService _settingsService;
        private readonly TaskbarIcon _taskbarIcon;

        public NotifyService(ILogger logger, IEventAggregator eventAggregator,
            FeedStorageService feedStorageService, SettingsService settingsService,
            NyaaService nyaaService, AnimeBytesService animeBytesService,
            TaskbarIcon taskbarIcon)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _feedStorageService = feedStorageService;
            _settingsService = settingsService;
            _nyaaService = nyaaService;
            _animeBytesService = animeBytesService;
            _taskbarIcon = taskbarIcon;
        }

        private void OnOnNyaaRssFeedItemsUpdatedEvent(object? sender, EventArgs e)
        {
            using var db = new TrackContext();
            var items = db.NotifyItems
                          .Where(i => i.Site.HasFlag(NotifySite.Nyaa))
                          .Select(i => i.Id)
                          .ToList();
            foreach (var item in items) _ = NyaaJob(item, _feedStorageService.NyaaRssFeedItems);
        }

        private void OnOnAnimeBytesRssFeedItemsUpdatedEvent(object? sender, EventArgs e)
        {
            using var db = new TrackContext();
            var items = db.NotifyItems
                          .Where(i => i.Site.HasFlag(NotifySite.AnimeBytes))
                          .Select(i => i.Id)
                          .ToList();
            foreach (var item in items) _ = AnimeBytesJob(item, _feedStorageService.AnimeBytesAllRssFeedItems);
        }

        public void Init()
        {
            _feedStorageService.OnAnimeBytesAllRssFeedItemsUpdatedEvent += OnOnAnimeBytesRssFeedItemsUpdatedEvent;
            _feedStorageService.OnNyaaRssFeedItemsUpdatedEvent += OnOnNyaaRssFeedItemsUpdatedEvent;
        }

        public void Stop()
        {
            _feedStorageService.OnAnimeBytesAllRssFeedItemsUpdatedEvent -= OnOnAnimeBytesRssFeedItemsUpdatedEvent;
            _feedStorageService.OnNyaaRssFeedItemsUpdatedEvent -= OnOnNyaaRssFeedItemsUpdatedEvent;
        }

        public async Task AnimeBytesJob(int id, List<AnimeBytesTorrentItem>? feedItems)
        {
            await using var db = new TrackContext();

            var item = await db.NotifyItems
                               .Include(n => n.Matches)
                               .Include(n => n.Keywords)
                               .FirstOrDefaultAsync(n => n.Id == id);
            if (item is null)
            {
                return;
            }

            feedItems ??= await _animeBytesService.GetFeedItems(AnimeBytesFilter.All, false);
            if (feedItems is null)
            {
                return;
            }

            var newMatch = 0;
            foreach (var feedItem in feedItems)
            {
                // Skip if we already matched this
                if (item.Matches?.FirstOrDefault(m => m.DownloadLink == feedItem.DownloadLink) is not null)
                {
                    continue;
                }

                var matchedKeywords = new List<NotifyItemKeyword>();
                foreach (var keyword in item.Keywords)
                {
                    var matched = IsMatch(keyword, feedItem.Name);
                    if (keyword.MustMatch && !matched)
                    {
                        // if it's a mustmatch item it will break and clear previous matches to get to
                        // the next feeditem
                        matchedKeywords.Clear();
                        break;
                    }

                    if (matched)
                    {
                        matchedKeywords.Add(keyword);
                    }
                }

                if (!matchedKeywords.Any())
                {
                    continue;
                }


                switch (item.MatchAll)
                {
                    case true when matchedKeywords.Count == item.Keywords.Count:
                        item.Matches?.Add(new NotifyItemMatch
                        {
                            Keywords = matchedKeywords.Select(k => k.Word).ToArray(),
                            Name = feedItem.Name,
                            Site = NotifySite.AnimeBytes,
                            Json = JsonConvert.SerializeObject(feedItem),
                            DownloadLink = feedItem.DownloadLink,
                            Link = feedItem.GroupUrl,
                        });
                        newMatch += 1;
                        break;
                    case false when matchedKeywords.Count > 0:
                        item.Matches?.Add(new NotifyItemMatch
                        {
                            Keywords = matchedKeywords.Select(k => k.Word).ToArray(),
                            Name = feedItem.Name,
                            Site = NotifySite.AnimeBytes,
                            Json = JsonConvert.SerializeObject(feedItem),
                            DownloadLink = feedItem.DownloadLink,
                            Link = feedItem.GroupUrl,
                        });
                        newMatch += 1;
                        break;
                }
            }

            if (newMatch > 0)
            {
                await SaveInDatabase(item);
                NotifyUser(item, newMatch);
                SendEvent(item);
            }
        }

        private bool IsMatch(NotifyItemKeyword keyword, string name)
        {
            if (keyword.IsRegex)
            {
                var regex = new Regex(keyword.Word,
                    keyword.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                if (regex.IsMatch(name))
                {
                    return true;
                }
            }

            if (name.Contains(keyword.Word,
                keyword.IsCaseSensitive
                    ? StringComparison.InvariantCulture
                    : StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static async Task SaveInDatabase(NotifyItem item)
        {
            await using var db = new TrackContext();
            db.Attach(item);
            db.Update(item);
            await db.SaveChangesAsync();
        }

        public async Task NyaaJob(int id, List<NyaaTorrentItem>? feedItems)
        {
            await using var db = new TrackContext();

            var item = await db.NotifyItems
                               .Include(n => n.Matches)
                               .Include(n => n.Keywords)
                               .FirstOrDefaultAsync(n => n.Id == id);
            if (item is null)
            {
                return;
            }

            feedItems ??= await _nyaaService.GetFeedItems(NyaaFilter.NoFilter, "", false);
            if (feedItems is null)
            {
                return;
            }

            var newMatch = 0;
            foreach (var feedItem in feedItems)
            {
                // Skip if we already matched this
                if (item.Matches?.FirstOrDefault(m => m.DownloadLink == feedItem.DownloadLink) != null)
                {
                    continue;
                }

                var matchedKeywords = new List<NotifyItemKeyword>();
                foreach (var keyword in item.Keywords)
                {
                    var matched = IsMatch(keyword, feedItem.Name);
                    if (keyword.MustMatch && !matched)
                    {
                        // if it's a mustmatch item it will break and clear previous matches to get to
                        // the next feeditem
                        matchedKeywords.Clear();
                        break;
                    }

                    if (matched)
                    {
                        matchedKeywords.Add(keyword);
                    }
                }

                if (!matchedKeywords.Any())
                {
                    continue;
                }


                switch (item.MatchAll)
                {
                    case true when matchedKeywords.Count == item.Keywords.Count:
                        item.Matches?.Add(new NotifyItemMatch
                        {
                            Keywords = matchedKeywords.Select(k => k.Word).ToArray(),
                            Name = feedItem.Name,
                            Site = NotifySite.Nyaa,
                            Json = JsonConvert.SerializeObject(feedItem),
                            DownloadLink = feedItem.DownloadLink,
                            Link = feedItem.Link,
                            NotifyItemId = id,
                        });
                        newMatch += 1;
                        break;
                    case false when matchedKeywords.Count > 0:
                        item.Matches?.Add(new NotifyItemMatch
                        {
                            Keywords = matchedKeywords.Select(k => k.Word).ToArray(),
                            Name = feedItem.Name,
                            Site = NotifySite.Nyaa,
                            Json = JsonConvert.SerializeObject(feedItem),
                            DownloadLink = feedItem.DownloadLink,
                            Link = feedItem.Link,
                            NotifyItemId = id,
                        });
                        newMatch += 1;
                        break;
                }
            }

            if (newMatch > 0)
            {
                await SaveInDatabase(item);
                NotifyUser(item, newMatch);
                SendEvent(item);
            }
        }

        private void SendEvent(NotifyItem item)
        {
            _eventAggregator.PublishOnUIThread(new NotifyItemAddOrUpdateEvent
            {
                Item = item,
                IsUpdate = true,
            });
        }

        private void NotifyUser(NotifyItem item, int matches)
        {
            _taskbarIcon.ShowBalloonTip(
                $"New {"match".ToQuantity(matches)} found!", $"Found {matches} new {"match".ToQuantity(matches, ShowQuantityAs.None)} for {item.Name}", 
                BalloonIcon.Info);
        }
    }
}