using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Anidow.Enums;
using Anidow.GraphQL;
using Anidow.Model;
using Humanizer;
using Stylet;

namespace Anidow.Database.Models
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Anime : ObservableObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public string Name { get; set; }
        public string Synopsis { get; set; }
        public int Score { get; set; }
        public int IdMal { get; set; }
        public string Genres { get; set; }
        public Site Site { get; set; }
        public string Folder { get; set; }
        public string Resolution { get; set; }
        public DateTime Released { get; set; }
        public string Cover { get; set; }
        public virtual Cover CoverData { get; set; }
        public string GroupId { get; set; }
        public string GroupUrl { get; set; }
        [Required] public string Group { get; set; }
        public AnimeStatus Status { get; set; }
        public virtual AniListAnime AniListAnime { get; set; }

        [NotMapped] public string ReleasedString => Released.Humanize();
        [NotMapped] public DateTime ReleasedLocal => Released.ToLocalTime();
        [NotMapped] public bool IsAiring => Status == AnimeStatus.Watching;
        [NotMapped] public bool IsFinished => Status == AnimeStatus.Completed;
        [NotMapped] public int Episodes => EpisodeList?.Count ?? 0;
        [NotMapped] public ICollection<Episode> EpisodeList { get; set; } = new BindableCollection<Episode>();
        [NotMapped] public ICollection<string> GenreList => new BindableCollection<string>(Genres?.Split(","));
        [NotMapped] public bool TrackedViewSelected { get; set; }
        [NotMapped] public bool HasInformation => AniListAnime is not null;
        [NotMapped] public string Notification { get; set; }
    }
}