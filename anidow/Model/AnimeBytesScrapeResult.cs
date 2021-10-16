using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Anidow.Extensions;
using Anidow.Interfaces;

// ReSharper disable InconsistentNaming

// ReSharper disable ClassNeverInstantiated.Global

namespace Anidow.Model
{
    public class AnimeBytesScrapeResult
    {
        public int Matches { get; set; }
        public int Limit { get; set; }
        public string Results { get; set; }
        public AnimeBytesScrapeAnime[] Groups { get; set; }
    }

    public class AnimeBytesScrapeAnime : ObservableObject
    {
        private string _folder;
        public int ID { get; set; }
        public int Row { get; set; }
        public string CategoryName { get; set; }
        public string FullName { get; set; }
        public string GroupName { get; set; }
        public string SeriesID { get; set; }
        public string SeriesName { get; set; }
        public object Artists { get; set; }
        public string Year { get; set; }
        public string Image { get; set; }
        public object Synonymns { get; set; }

        [JsonIgnore] public List<string> SynonymnsList { get; set; }

        public int Snatched { get; set; }
        public int Comments { get; set; }
        public object Links { get; set; }

        [JsonIgnore] public Dictionary<string, string> LinksDict { get; set; }

        public int Votes { get; set; }
        public float AvgVote { get; set; }
        public object Associations { get; set; }
        public string Description { get; set; }
        public string DescriptionHTML { get; set; }
        public int EpCount { get; set; }
        public string StudioList { get; set; }
        public int PastWeek { get; set; }
        public bool Incomplete { get; set; }
        public bool Ongoing { get; set; }
        public string[] Tags { get; set; }
        public AnimeBytesScrapeTorrent[] Torrents { get; set; }
        [JsonIgnore] public string TagsString => Tags != null ? string.Join(", ", Tags) : null;
        [JsonIgnore] public int SelectedTorrentIndex { get; set; }
        [JsonIgnore] public AnimeBytesScrapeTorrent SelectedTorrent { get; set; }

        [JsonIgnore]
        public string Folder
        {
            get => _folder;
            set
            {
                _folder = value;
                foreach (var torrent in Torrents)
                {
                    torrent.Folder = value;
                }
            }
        }

        [JsonIgnore] public bool CanTrack { get; set; }

        [JsonIgnore] public string SelectedSubGroup { get; set; }

        [JsonIgnore]
        public List<string> SubGroups
        {
            get
            {
                if (Torrents is null or {Length: 0})
                {
                    return null;
                }

                var list = new List<string>();
                foreach (var torrent in Torrents)
                {
                    var group = torrent.GetReleaseGroup();
                    var resolution = torrent.GetResolution();
                    var s = $"{group} | {resolution}";
                    if (!string.IsNullOrWhiteSpace(group) && !string.IsNullOrWhiteSpace(resolution) &&
                        !list.Contains(s))
                    {
                        list.Add(s);
                    }
                }

                SelectedSubGroup = list.FirstOrDefault();
                return list;
            }
        }

        [JsonIgnore] public bool CanDownload { get; set; } = true;
        [JsonIgnore] public bool IsTracked { get; set; }
    }

    public class AnimeBytesScrapeTorrent : ObservableObject, ITorrentItem
    {
        public int ID { get; set; }
        public AnimeBytesScrapeEditionData EditionData { get; set; }
        public float RawDownMultiplier { get; set; }
        public float RawUpMultiplier { get; set; }
        public string Property { get; set; }
        public int Snatched { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
        public string UploadTime { get; set; }

        [JsonIgnore]
        public string Name
        {
            get
            {
                var name = Property;
                if (EditionData != null && !string.IsNullOrEmpty(EditionData.EditionTitle))
                {
                    name = $"{EditionData.EditionTitle} {Property}";
                }

                return name;
            }
        }

        [JsonPropertyName("Link")] public string DownloadLink { get; set; }

        [JsonIgnore] public string Folder { get; set; }

        public class AnimeBytesScrapeEditionData
        {
            public string EditionTitle { get; set; }
            public string ReleaseDate { get; set; }
        }
    }
}