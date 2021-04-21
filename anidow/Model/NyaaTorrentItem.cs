using System;
using Anidow.Enums;
using Anidow.Interfaces;
using Humanizer;
using Stylet;

namespace Anidow.Model
{
    public class NyaaTorrentItem : PropertyChangedBase, ITorrentItem
    {
        public string Name { get; set; }
        public NyaaQuality Quality { get; set; } = NyaaQuality.Normal;
        public string Link { get; set; }
        public DateTime Released { get; set; }
        public DateTime ReleasedLocal => Released.ToLocalTime();
        public string Added => Released.Humanize();
        public string Seeders { get; set; }
        public string Leechers { get; set; }
        public string Size { get; set; }
        public string Downloads { get; set; }
        public string DownloadLink { get; set; }
        public string Folder { get; set; }
    }
}