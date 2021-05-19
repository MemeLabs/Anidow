using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Anidow.Database.Models;
using Anidow.Interfaces;
using Anidow.Model;
using Anidow.Services;
using Serilog;

namespace Anidow.Torrent_Clients
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class QBitTorrent : IBaseTorrentClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly SettingsService _settingsService;

        public QBitTorrent(ILogger logger, HttpClient httpClient, SettingsService settingsService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _settingsService = settingsService;
        }

        private SettingsModel Settings => _settingsService.Settings;
        private string ApiUrl => $"{Settings.QBitTorrent.Host}:{Settings.QBitTorrent.Port}";
        private bool LoggedIn { get; set; }


        public async Task<bool> Add(ITorrentItem item)
        {
            await Login();
            var m = new MultipartFormDataContent(Guid.NewGuid().ToString())
            {
                {new StringContent(item.DownloadLink), "urls"},
                {new StringContent(item.Folder), "savepath"},
                {new StringContent(Settings.QBitTorrent.Category), "category"},
            };
            var response = await _httpClient.PostAsync($"{ApiUrl}/api/v2/torrents/add", m);
            if (response != null && response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.Information(content);
                return true;
            }

            if (response != null)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.Error(content);
            }

            _logger.Information($"failed adding {item.DownloadLink} to qbittorrent");
            return false;
        }

        public async Task<bool> Remove(Episode episode, bool withFile = false)
        {
            if (string.IsNullOrEmpty(episode.TorrentId))
            {
                return true;
            }

            await Login();
            // /api/v2/torrents/delete?hashes=8c212779b4abde7c6bc608063a0d008b7e40ce32&deleteFiles=false
            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"{ApiUrl}/api/v2/torrents/delete?hashes={episode.TorrentId}&deleteFiles={withFile}");
                return response?.IsSuccessStatusCode ?? false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed deleting file from qbittorrent");
                return false;
            }
        }

        private async Task Login()
        {
            if (LoggedIn || !Settings.QBitTorrent.WithLogin)
            {
                return;
            }

            var data = new Dictionary<string, string>
            {
                {"username", Settings.QBitTorrent.Username},
                {"password", Settings.QBitTorrent.Password},
            };
            var oldReferer = _httpClient.DefaultRequestHeaders.Referrer;
            _httpClient.DefaultRequestHeaders.Referrer = new Uri(ApiUrl);
            var login = await _httpClient.PostAsync($"{ApiUrl}/api/v2/auth/login", new FormUrlEncodedContent(data));
            LoggedIn = login.IsSuccessStatusCode;
            _httpClient.DefaultRequestHeaders.Referrer = oldReferer;
        }

        public async Task<List<T>> GetTorrentList<T>()
        {
            await Login();
            // /api/v2/torrents/info?filter=all&category=Anime&sort=added%20on
            try
            {
                var response =
                    await _httpClient.GetStringAsync(
                        $"{ApiUrl}/api/v2/torrents/info?filter=all&category={Settings.QBitTorrent.Category.ToLower()}&sort=added%20on");
                var list = JsonSerializer.Deserialize<List<T>>(response);
                return list;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed getting torrent list from qbittorrent");
                return default;
            }
        }
    }
}