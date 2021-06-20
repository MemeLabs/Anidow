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
            AnimeBytesRssFeedItems = new List<AnimeBytesTorrentItem>();
            AnimeBytesSearchFeedItems = new List<AnimeBytesScrapeAnime>();
            NyaaRssFeedItems = new List<NyaaTorrentItem>();
        }

        public event EventHandler OnAnimeBytesRssFeedItemsUpdatedEvent;
        public event EventHandler OnAnimeBytesSearchFeedItemsUpdatedEvent;
        public event EventHandler OnNyaaRssFeedItemsUpdatedEvent;
        
        public List<AnimeBytesTorrentItem> AnimeBytesRssFeedItems { get; set; }
        public List<AnimeBytesScrapeAnime> AnimeBytesSearchFeedItems { get; set; }
        public List<NyaaTorrentItem> NyaaRssFeedItems { get; set; }

        public void SetAnimeBytesRssFeedItems(IEnumerable<AnimeBytesTorrentItem> items)
        {
            AnimeBytesRssFeedItems.Clear();
            AnimeBytesRssFeedItems.AddRange(items);
            OnAnimeBytesRssFeedItemsUpdatedEvent?.Invoke(this, EventArgs.Empty);
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