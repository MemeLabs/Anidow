using System.IO;
using Anidow.Enums;

namespace Anidow.Model
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsModel : ObservableObject
    {
        public string AnimeFolder { get; set; } = Directory.GetCurrentDirectory();
        public int RefreshTime { get; set; } = 5;
        public bool IsDark { get; set; } = true;
        public bool Notifications { get; set; } = true;
        public TorrentClient TorrentClient { get; set; } = TorrentClient.QBitTorrent;
        public QBitTorrentSettingsModel QBitTorrent { get; set; } = new QBitTorrentSettingsModel();
        public AnimeBytesSettingsModel AnimeBytesSettings { get; set; } = new AnimeBytesSettingsModel();
        public NyaaSettingsModel NyaaSettings { get; set; } = new NyaaSettingsModel();
    }
}