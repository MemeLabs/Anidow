using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Factories;
using Anidow.Interfaces;
using Anidow.Model;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Serilog;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TorrentService
    {
        private readonly TorrentClientFactory _clientFactory;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsModel _settings;
        private readonly BencodeParser _bencodeParser = new();

        public TorrentService(SettingsModel settings, TorrentClientFactory clientFactory,
            HttpClient httpClient, ILogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool, Torrent)> Download(ITorrentItem item)
        {
            var torrent = await ParseTorrentFile(item.DownloadLink);
#if RELEASE
            var result = _settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => await _clientFactory.GetQBitTorrent.Add(item),
                TorrentClient.Deluge => throw new NotImplementedException(),
                _ => false,
            };
            return (result, torrent);
#else
            return (true, torrent);
#endif
        }

        private async Task<Torrent> ParseTorrentFile(string itemDownloadLink)
        {
            try
            {
                var response = await _httpClient.GetStreamAsync(itemDownloadLink);
                var torrent = _bencodeParser.Parse<Torrent>(response);
                return torrent;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed parsing torrent file");
                return null;
            }
        }

        public async Task<bool> Remove(Episode anime, bool withFile = false)
        {
            return _settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => await _clientFactory.GetQBitTorrent.Remove(anime, withFile),
                TorrentClient.Deluge => throw new NotImplementedException(),
                _ => false,
            };
        }

        public async Task<List<T>> GetTorrents<T>()
        {
            return _settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => await _clientFactory.GetQBitTorrent.GetTorrentList<T>(),
                TorrentClient.Deluge => throw new NotImplementedException(),
                _ => default,
            };
        }
    }
}