using System;
using System.IO;
using Anidow.Enums;

namespace Anidow.Model
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [Serializable]
    public class SettingsModel : ObservableObject
    {
        public string AnimeFolder { get; set; } = Directory.GetCurrentDirectory();
        public int RefreshTime { get; set; } = 5;
        public bool IsDark { get; set; } = true;
        public bool Notifications { get; set; } = true;
        public bool TrackedIsCardView { get; set; }
        public TorrentClient TorrentClient { get; set; } = TorrentClient.QBitTorrent;
        public QBitTorrentSettingsModel QBitTorrent { get; set; } = new();
        public AnimeBytesSettingsModel AnimeBytesSettings { get; set; } = new();
        public NyaaSettingsModel NyaaSettings { get; set; } = new();
    }
}