// // Created: 17-06-2021 12:38

using System;
using System.Collections.Generic;
using Anidow.Model;

namespace Anidow.Services
{
    public class FeedStorageService
    {
        public FeedStorageService()
        {
            AnimeBytesAiringRssFeedItems = new List<AnimeBytesTorrentItem>();
            AnimeBytesAllRssFeedItems = new List<AnimeBytesTorrentItem>();
            AnimeBytesSearchFeedItems = new List<AnimeBytesScrapeAnime>();
            NyaaRssFeedItems = new List<NyaaTorrentItem>();
        }

        public event EventHandler OnAnimeBytesAiringRssFeedItemsUpdatedEvent;
        public event EventHandler OnAnimeBytesAllRssFeedItemsUpdatedEvent;
        public event EventHandler OnAnimeBytesSearchFeedItemsUpdatedEvent;
        public event EventHandler OnNyaaRssFeedItemsUpdatedEvent;
        
        public List<AnimeBytesTorrentItem> AnimeBytesAiringRssFeedItems { get; set; }
        public List<AnimeBytesTorrentItem> AnimeBytesAllRssFeedItems { get; set; }
        public List<AnimeBytesScrapeAnime> AnimeBytesSearchFeedItems { get; set; }
        public List<NyaaTorrentItem> NyaaRssFeedItems { get; set; }

        public void SetAnimeBytesAiringRssFeedItems(IEnumerable<AnimeBytesTorrentItem> items)
        {
            AnimeBytesAiringRssFeedItems.Clear();
            AnimeBytesAiringRssFeedItems.AddRange(items);
            OnAnimeBytesAiringRssFeedItemsUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }

        public void SetAnimeBytesAllRssFeedItems(IEnumerable<AnimeBytesTorrentItem> items)
        {
            AnimeBytesAllRssFeedItems.Clear();
            AnimeBytesAllRssFeedItems.AddRange(items);
            OnAnimeBytesAllRssFeedItemsUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }
        
        public void SetAnimeBytesSearchFeedItems(IEnumerable<AnimeBytesScrapeAnime> items)
        {
            AnimeBytesSearchFeedItems.Clear();
            AnimeBytesSearchFeedItems.AddRange(items);
            OnAnimeBytesSearchFeedItemsUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }
        
        public void SetNyaaRssFeedItems(IEnumerable<NyaaTorrentItem> items)
        {
            NyaaRssFeedItems.Clear();
            NyaaRssFeedItems.AddRange(items);
            OnNyaaRssFeedItemsUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}