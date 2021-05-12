using System;

namespace Anidow.Interfaces
{
    public interface IEpisode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Folder { get; set; }
        public string File { get; set; }
        public string TorrentId { get; set; }
        public string DownloadLink { get; set; }
        public DateTime Released { get; set; }
        public bool Watched { get; set; }
        public DateTime WatchedDate { get; set; }
        public bool Hide { get; set; }
        public string Link { get; set; }

        public string ReleasedString { get; }
        public string WatchedString { get; }
        public string EpisodeNum { get; }

        public string AnimeId { get; set; }
    }
}