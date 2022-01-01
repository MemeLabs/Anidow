using System;
using System.ComponentModel.DataAnnotations.Schema;
using Anidow.Enums;
using Anidow.Extensions;
using Anidow.Interfaces;
using Anidow.Model;
using Humanizer;

namespace Anidow.Database.Models;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class Episode : ObservableObject, ITorrentItem, IEpisode
{
    public Site Site { get; set; }
    public string Cover { get; set; }
    public Cover CoverData { get; set; }
    [NotMapped] public DateTime ReleasedLocal => Released.ToLocalTime();
    [NotMapped] public DateTime WatchedDateLocal => WatchedDate.ToLocalTime();
    [NotMapped] public string WatchedHeaderString => Watched ? "Not watched" : "Watched";
    [NotMapped] public bool HasFile => !string.IsNullOrWhiteSpace(File);
    [NotMapped] public bool TorrentFinished => TorrentProgress >= 1.00f;
    [NotMapped] public string TorrentProgressContent => $"{TorrentProgress / 1f * 100:0.#}%";

    [NotMapped]
    public bool TorrentShowProgress => !string.IsNullOrWhiteSpace(TorrentId)
                                       && TorrentProgress is > 0f and < 1f;

    public DateTime Created { get; set; } = DateTime.Now;
    [NotMapped] public bool CanOpen { get; set; } = true;
    [NotMapped] public bool HasAniListAnime => Anime?.AniListAnime is not null;
    [NotMapped] public bool HasAnime => Anime is not null;
    [NotMapped] public Anime Anime { get; set; }
    public DateTime HideDate { get; set; }
    [NotMapped] public float TorrentProgress { get; set; }

    public int Id { get; set; }
    public string Name { get; set; }
    public string File { get; set; }
    public string TorrentId { get; set; }
    public DateTime Released { get; set; }
    public bool Watched { get; set; }
    public DateTime WatchedDate { get; set; }
    public bool Hide { get; set; }
    public string Link { get; set; }
    [NotMapped] public bool IsFuture { get; set; }
    [NotMapped] public string ReleasedString => Released.Humanize();
    [NotMapped] public string WatchedString => WatchedDate == default ? string.Empty : WatchedDate.Humanize();
    [NotMapped] public string EpisodeNum => Name.GetEpisode();
    [NotMapped] public bool HomeHighlight { get; set; }
    [NotMapped] public string AnimeName => Anime?.Name ?? string.Empty;

    public string AnimeId { get; set; }
    public string Folder { get; set; }
    public string DownloadLink { get; set; }
}