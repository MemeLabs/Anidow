using System;
using System.ComponentModel.DataAnnotations.Schema;
using Anidow.Enums;
using Anidow.Extensions;
using Anidow.Interfaces;
using Anidow.Model;
using Humanizer;

namespace Anidow.Database.Models
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Episode : ObservableObject, ITorrentItem, IEpisode
    {
        public Site Site { get; set; }
        public DateTime HideDate { get; set; }
        public string Cover { get; set; }
        public virtual Cover CoverData { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
        public string TorrentId { get; set; }
        public DateTime Released { get; set; }
        public bool Watched { get; set; }
        public DateTime WatchedDate { get; set; }
        public bool Hide { get; set; }
        public string Link { get; set; }

        [NotMapped] public string ReleasedString => Released.Humanize();
        [NotMapped] public string WatchedString => WatchedDate == default ? string.Empty : WatchedDate.Humanize();
        [NotMapped] public string WatchedHeaderString => Watched ? "Not watched" : "Watched";
        [NotMapped] public string EpisodeNum => Name.GetEpisode();

        public string AnimeId { get; set; }
        public string Folder { get; set; }
        public string DownloadLink { get; set; }
    }
}