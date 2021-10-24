using System.Text.Json.Serialization;

namespace Anidow.Model;

public class Api
{
    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("compat")] public int Compat { get; set; }
}

public class Freeleech
{
    [JsonPropertyName("sitewide")] public int Sitewide { get; set; }

    [JsonPropertyName("personal")] public int Personal { get; set; }
}

public class Torrents
{
    [JsonPropertyName("active")] public int Active { get; set; }

    [JsonPropertyName("all")] public int All { get; set; }

    [JsonPropertyName("snatches")] public int Snatches { get; set; }

    [JsonPropertyName("seeding")] public int Seeding { get; set; }

    [JsonPropertyName("leeching")] public int Leeching { get; set; }

    [JsonPropertyName("snatched")] public int Snatched { get; set; }

    [JsonPropertyName("uploaded")] public int Uploaded { get; set; }

    [JsonPropertyName("pruned")] public int Pruned { get; set; }

    [JsonPropertyName("ssize")] public long Ssize { get; set; }

    [JsonPropertyName("sttime")] public long Sttime { get; set; }

    [JsonPropertyName("satime")] public int Satime { get; set; }
}

public class Requests
{
    [JsonPropertyName("filled")] public int Filled { get; set; }

    [JsonPropertyName("all")] public int All { get; set; }
}

public class Users
{
    [JsonPropertyName("enabled")] public int Enabled { get; set; }

    [JsonPropertyName("now")] public int Now { get; set; }

    [JsonPropertyName("day")] public int Day { get; set; }

    [JsonPropertyName("week")] public int Week { get; set; }

    [JsonPropertyName("month")] public int Month { get; set; }

    [JsonPropertyName("irc")] public int Irc { get; set; }
}

public class PeersUniq
{
    [JsonPropertyName("seeders")] public int Seeders { get; set; }

    [JsonPropertyName("leechers")] public int Leechers { get; set; }
}

public class Peers
{
    [JsonPropertyName("seeders")] public int Seeders { get; set; }

    [JsonPropertyName("leechers")] public int Leechers { get; set; }
}

public class Classes
{
    [JsonPropertyName("Aka-chan")] public int AkaChan { get; set; }

    [JsonPropertyName("User")] public int User { get; set; }

    [JsonPropertyName("Power User")] public int PowerUser { get; set; }

    [JsonPropertyName("Elite")] public int Elite { get; set; }

    [JsonPropertyName("Torrent Master")] public int TorrentMaster { get; set; }

    [JsonPropertyName("Legend")] public int Legend { get; set; }

    [JsonPropertyName("VIP")] public int VIP { get; set; }

    [JsonPropertyName("Sensei")] public int Sensei { get; set; }
}

public class Forums
{
    [JsonPropertyName("posts")] public int Posts { get; set; }

    [JsonPropertyName("topics")] public int Topics { get; set; }
}

public class Donations
{
    [JsonPropertyName("currency")] public string Currency { get; set; }

    [JsonPropertyName("goal")] public int Goal { get; set; }

    [JsonPropertyName("collected")] public int Collected { get; set; }
}

public class ABSite
{
    [JsonPropertyName("torrents")] public Torrents Torrents { get; set; }

    [JsonPropertyName("requests")] public Requests Requests { get; set; }

    [JsonPropertyName("users")] public Users Users { get; set; }

    [JsonPropertyName("peers_uniq")] public PeersUniq PeersUniq { get; set; }

    [JsonPropertyName("peers")] public Peers Peers { get; set; }

    [JsonPropertyName("classes")] public Classes Classes { get; set; }

    [JsonPropertyName("forums")] public Forums Forums { get; set; }

    [JsonPropertyName("donations")] public Donations Donations { get; set; }
}

public class Yen
{
    [JsonPropertyName("day")] public int Day { get; set; }

    [JsonPropertyName("hour")] public int Hour { get; set; }

    [JsonPropertyName("now")] public int Now { get; set; }
}

public class Hnrs
{
    [JsonPropertyName("potential")] public int Potential { get; set; }

    [JsonPropertyName("active")] public int Active { get; set; }
}

public class Upload
{
    [JsonPropertyName("raw")] public long Raw { get; set; }

    [JsonPropertyName("accountable")] public long Accountable { get; set; }
}

public class Download
{
    [JsonPropertyName("raw")] public long Raw { get; set; }

    [JsonPropertyName("accountable")] public long Accountable { get; set; }
}

public class Personal
{
    [JsonPropertyName("yen")] public Yen Yen { get; set; }

    [JsonPropertyName("hnrs")] public Hnrs Hnrs { get; set; }

    [JsonPropertyName("upload")] public Upload Upload { get; set; }

    [JsonPropertyName("download")] public Download Download { get; set; }

    [JsonPropertyName("torrents")] public Torrents Torrents { get; set; }

    [JsonPropertyName("invited")] public int Invited { get; set; }

    [JsonPropertyName("forums")] public Forums Forums { get; set; }

    [JsonPropertyName("pcomments")] public int Pcomments { get; set; }

    [JsonPropertyName("class")] public string Class { get; set; }
}

public class Stats
{
    [JsonPropertyName("site")] public ABSite Site { get; set; }

    [JsonPropertyName("personal")] public Personal Personal { get; set; }
}

public class AnimeBytesStatsResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("git")] public string Git { get; set; }

    [JsonPropertyName("api")] public Api Api { get; set; }

    [JsonPropertyName("freeleech")] public Freeleech Freeleech { get; set; }

    [JsonPropertyName("stats")] public Stats Stats { get; set; }
}