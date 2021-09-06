using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;

namespace Anidow.Services
{
    public class RssFeedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        protected RssFeedService(ILogger logger, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        protected async Task<List<T>> GetFeedItems<T>(string url, Func<SyndicationItem, T> func)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.Warning($"GetFeedItems - empty url '{url}'");
                return null;
            }

            var feed = ParseRssFeed(await GetRssFeedStringAsync(url));

            return feed?.Items.Select(func).ToList();
        }

        private async Task<Stream> GetRssFeedStringAsync(string url)
        {
            _logger.Debug($"getting rss feed items from {url}");
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response is not {IsSuccessStatusCode: true})
                {
                    _logger.Warning($"getting {url} returned null");
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warning($"getting {url} returned unsuccessful status code {response.StatusCode}");
                    return null;
                }

                if (response.Content.Headers.ContentType == null)
                {
                    _logger.Warning($"got empty ContentType {url}");
                    return null;
                }

                var mediaType = response.Content.Headers.ContentType.MediaType;

                if (mediaType == "application/xml")
                {
                    var content = await response.Content.ReadAsStreamAsync();

                    return content;
                }

                _logger.Error($"wrong content-type, expected 'application/xml' got '{mediaType}'");

                return null;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed getting rss feed");
                return null;
            }
        }

        private SyndicationFeed ParseRssFeed(Stream contentStream)
        {
            if (contentStream is null)
            {
                return null;
            }
            try
            {
                var xmlReader = XmlReader.Create(contentStream);
                var feed = SyndicationFeed.Load(xmlReader);
                return feed;
            }
            catch (Exception e)
            {
                _logger.Error(e, "error parsing xml content");
                return null;
            }
        }

        protected string GetElementExtensionValue(SyndicationItem item, string value)
        {
            return item.ElementExtensions.FirstOrDefault(e => e.OuterName == value)
                       ?.GetObject<XElement>().Value;
        }
    }
}