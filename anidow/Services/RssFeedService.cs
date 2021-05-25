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
                _logger.Information("GetFeedItems - empty url");
                return default;
            }

            var content = await GetRssFeedStringAsync(url);
            if (content == default)
            {
                return default;
            }

            var feed = await Task.Run(() => ParseRssFeed(content));

            return feed?.Items.Select(func).ToList();
        }

        private async Task<string> GetRssFeedStringAsync(string url)
        {
            _logger.Information($"getting rss feed items from {url}");
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response is not {IsSuccessStatusCode: true})
                {
                    _logger.Information($"getting {url} returned null");
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Information($"getting {url} returned unsuccessful status code {response.StatusCode}");
                    return default;
                }

                if (response.Content.Headers.ContentType == null)
                {
                    _logger.Warning($"got empty ContentType {url}");
                    return default;
                }

                var mediaType = response.Content.Headers.ContentType.MediaType;

                if (mediaType == "application/xml")
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return content;
                }

                _logger.Error($"wrong content-type, expected 'application/xml' got '{mediaType}'");

                return default;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed getting rss feed");
                return default;
            }
        }

        private SyndicationFeed ParseRssFeed(string content)
        {
            try
            {
                var xmlReader = XmlReader.Create(new StringReader(content));
                var feed = SyndicationFeed.Load(xmlReader);
                return feed;
            }
            catch (Exception e)
            {
                _logger.Error(e, "error parsing xml content");
                return default;
            }
        }

        protected string GetElementExtensionValue(SyndicationItem item, string value)
        {
            return item.ElementExtensions.FirstOrDefault(e => e.OuterName == value)
                       ?.GetObject<XElement>().Value;
        }
    }
}