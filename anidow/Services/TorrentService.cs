using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Factories;
using Anidow.Interfaces;
using Anidow.Torrent_Clients;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Serilog;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TorrentService
    {
        private readonly BencodeParser _bencodeParser = new();
        private readonly TorrentClientFactory _clientFactory;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;

        public TorrentService(SettingsService settingsService, TorrentClientFactory clientFactory,
            HttpClient httpClient, ILogger logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool, Torrent)> Download(ITorrentItem item)
        {
            var torrent = await ParseTorrentFile(item.DownloadLink);
#if RELEASE
            var result = _settingsService.Settings.TorrentClient switch
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
            return _settingsService.Settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => await _clientFactory.GetQBitTorrent.Remove(anime, withFile),
                TorrentClient.Deluge => throw new NotImplementedException(),
                _ => false,
            };
        }

        private async Task<T> GetTorrents<T>()
        {
            return _settingsService.Settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => await _clientFactory.GetQBitTorrent.GetTorrentList<T>(),
                TorrentClient.Deluge => throw new NotImplementedException(),
                _ => default,
            };
        }

        public async Task UpdateTorrentProgress(IEnumerable<Episode> items)
        {
            var torrents = _settingsService.Settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => (object[]) await GetTorrents<QBitTorrentEntry[]>(),
                TorrentClient.Deluge => null,
                _ => null,
            };

            if (torrents is null)
            {
                _logger.Debug("finished job: UpdateTorrents");
                return;
            }

            switch (_settingsService.Settings.TorrentClient)
            {
                case TorrentClient.QBitTorrent:
                    var torrentItems = (QBitTorrentEntry[]) torrents;
                    foreach (var anime in items)
                    {
                        if (string.IsNullOrWhiteSpace(anime.TorrentId))
                        {
                            continue;
                        }

                        var torrent = torrentItems.SingleOrDefault(t =>
                            t.hash.Equals(anime.TorrentId, StringComparison.InvariantCultureIgnoreCase));
                        
                        if (torrent is null)
                        {
                            continue;
                        }

                        anime.TorrentProgress = torrent.progress;
                    }

                    break;
                case TorrentClient.Deluge:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}