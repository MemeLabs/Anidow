using System;
using Anidow.Enums;
using Anidow.Interfaces;
using Humanizer;
using Newtonsoft.Json;

namespace Anidow.Model;

public class NyaaTorrentItem : ObservableObject, ITorrentItem
{
    public string Name { get; set; }
    public NyaaQuality Quality { get; set; } = NyaaQuality.Normal;
    public string Link { get; set; }
    public DateTime Released { get; set; }
    [JsonIgnore] public DateTime ReleasedLocal => Released.ToLocalTime();
    [JsonIgnore] public string Added => Released.Humanize();
    public int Seeders { get; set; }
    public int Leechers { get; set; }
    public string Size { get; set; }
    public string Downloads { get; set; }
    public bool CanDownload { get; set; } = true;
    public string DownloadLink { get; set; }
    public string Folder { get; set; }
}