using System;
using Anidow.Extensions;
using Anidow.Interfaces;
using Humanizer;
using Newtonsoft.Json;
using Stylet;

namespace Anidow.Model
{
    public class AnimeBytesTorrentItem : ObservableObject, ITorrentItem
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime Released { get; set; }
        [JsonIgnore] public DateTime ReleasedLocal => Released.ToLocalTime();
        public string Added => Released.Humanize();
        public string Size { get; set; }
        public string Cover { get; set; }
        public string GroupTitle { get; set; }
        public string GroupUrl { get; set; }
        public string TorrentProperty { get; set; }
        public bool CanTrack { get; set; }
        [JsonIgnore] public string Episode => this.GetEpisode();
        [JsonIgnore] public int EpisodeInt => this.GetEpisodeInt();
        [JsonIgnore] public string Resolution => this.GetResolution();
        public bool ShowInList { get; set; } = true;
        public string DownloadLink { get; set; }
        public string Folder { get; set; }
        public bool CanDownload { get; set; } = true;
    }
}