using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml.Linq;
using Anidow.Enums;
using Anidow.Model;
using Serilog;

namespace Anidow.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class NyaaService : RssFeedService
{
    private readonly FeedStorageService _feedStorageService;
    private readonly SettingsService _settingsService;

    public NyaaService(ILogger logger, HttpClient httpClient, SettingsService settingsService,
        FeedStorageService feedStorageService)
        : base(logger, httpClient)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _feedStorageService = feedStorageService ?? throw new ArgumentNullException(nameof(feedStorageService));
    }

    private SettingsModel Settings => _settingsService.Settings;

    public async Task<List<NyaaTorrentItem>> GetFeedItems(NyaaFilter filter, string search = "",
        bool addToFeedStorage = true)
    {
        var url = filter switch
        {
            NyaaFilter.NoFilter => $"https://nyaa.si/?page=rss&c=1_2&f=0&q={search}",
            NyaaFilter.NoRemakes => $"https://nyaa.si/?page=rss&c=1_2&f=1&q={search}",
            NyaaFilter.TrustedOnly => $"https://nyaa.si/?page=rss&c=1_2&f=2&q={search}",
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
        };

        var items = await GetFeedItems(url, ToDomain) ?? new List<NyaaTorrentItem>();
        var minSeeders = _settingsService.Settings.NyaaSettings.HideTorrentsBelowSeeders;
        if (minSeeders > -1)
        {
            return items.Where(i => i.Seeders >= minSeeders).ToList();
        }

        if (addToFeedStorage)
        {
            _feedStorageService.SetNyaaRssFeedItems(items);
        }

        return items;
    }


    private NyaaTorrentItem ToDomain(SyndicationItem item)
    {
        var feedItem = new NyaaTorrentItem
        {
            Released = item.PublishDate.DateTime,
            Name = item.Title.Text,
            DownloadLink = item.Links[0].Uri.AbsoluteUri,
            Link = item.Id,
            Folder = Settings.AnimeFolder,
            Seeders = int.Parse(item.ElementExtensions.FirstOrDefault(e => e.OuterName == "seeders")
                                    ?.GetObject<XElement>().Value ?? "0"),
            Leechers = int.Parse(item.ElementExtensions.FirstOrDefault(e => e.OuterName == "leechers")
                                     ?.GetObject<XElement>().Value ?? "0"),
            Size = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "size")
                       ?.GetObject<XElement>().Value,
            Downloads = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "downloads")
                            ?.GetObject<XElement>().Value,
        };


        var remake = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "remake")
                         ?.GetObject<XElement>().Value == "Yes";
        if (remake)
        {
            feedItem.Quality = NyaaQuality.Remake;
        }

        var trusted = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "trusted")
                          ?.GetObject<XElement>().Value == "Yes";
        if (trusted)
        {
            feedItem.Quality = NyaaQuality.Trusted;
        }

        return feedItem;
    }
}