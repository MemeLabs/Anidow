using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
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
        private string ApiUrl (SettingsModel settings) => $"{settings.QBitTorrent.Host}:{settings.QBitTorrent.Port}";
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
            var response = await _httpClient.PostAsync($"{ApiUrl(Settings)}/api/v2/torrents/add", m);
            var content = await response?.Content?.ReadAsStringAsync();
            if (response is {IsSuccessStatusCode: true})
            {
                _logger.Information(content);
                return true;
            }
            
            _logger.Error(content);

            _logger.Information($"failed adding {item.DownloadLink} to qbittorrent");
            return false;
        }

        public async Task<bool> Remove(IEpisode episode, bool withFile = false)
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
                        $"{ApiUrl(Settings)}/api/v2/torrents/delete?hashes={episode.TorrentId}&deleteFiles={withFile}");
                return response?.IsSuccessStatusCode ?? false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed deleting file from qbittorrent");
                return false;
            }
        }

        public async Task<bool> Login(SettingsModel settings = null)
        {
            settings ??= Settings;
            if (LoggedIn || !settings.QBitTorrent.WithLogin)
            {
                return false;
            }

            var data = new Dictionary<string, string>
            {
                {"username", settings.QBitTorrent.Username},
                {"password", settings.QBitTorrent.Password},
            };
            var oldReferer = _httpClient.DefaultRequestHeaders.Referrer;
            _httpClient.DefaultRequestHeaders.Referrer = new Uri(ApiUrl(settings));
            var login = await _httpClient.PostAsync($"{ApiUrl(settings)}/api/v2/auth/login", new FormUrlEncodedContent(data));
            LoggedIn = login.IsSuccessStatusCode;
            _httpClient.DefaultRequestHeaders.Referrer = oldReferer;
            return login.IsSuccessStatusCode;
        }

        public async Task<T> GetTorrentList<T>(SettingsModel settings = null)
        {
            settings ??= Settings;
            await Login(settings);
            // /api/v2/torrents/info?category=sample%20category&sort=ratio
            var encodedCategory = HttpUtility.UrlEncode(Settings.QBitTorrent.Category);
            var url = $"{ApiUrl(settings)}/api/v2/torrents/info?filter=all&category={encodedCategory}&sort=added_on";
            try
            {
                var response = await _httpClient.GetByteArrayAsync(url);
                var list = JsonSerializer.Deserialize<T>(response);
                return list;
            }
            catch (Exception e)
            {
                _logger.Error(e, "failed getting torrent list from qbittorrent");
                return default;
            }
        }

        public async Task<string> GetVersion()
        {
            var url = $"{ApiUrl(Settings)}/api/v2/app/version";
            return await _httpClient.GetStringAsync(url);
        }
    }
}