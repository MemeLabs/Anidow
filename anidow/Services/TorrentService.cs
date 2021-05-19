using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anidow.Database.Models;
using Anidow.Enums;
using Anidow.Factories;
using Anidow.Interfaces;
using Anidow.Model;

namespace Anidow.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TorrentService
    {
        private readonly TorrentClientFactory _clientFactory;
        private readonly SettingsModel _settings;

        public TorrentService(SettingsModel settings, TorrentClientFactory clientFactory)
        {
            _settings = settings;
            _clientFactory = clientFactory;
        }

        public async Task<bool> Download(ITorrentItem item)
        {
#if RELEASE
            return _settings.TorrentClient switch
            {
                TorrentClient.QBitTorrent => await _clientFactory.GetQBitTorrent.Add(item),
                TorrentClient.Deluge => throw new NotImplementedException(),
                _ => false
            };
#else
            return true;
#endif
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