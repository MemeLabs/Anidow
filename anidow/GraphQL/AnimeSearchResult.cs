using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using Anidow.Database.Models;
using Anidow.Utils;

namespace Anidow.GraphQL;

public class PageInfo
{
    [JsonPropertyName("total")] public int Total { get; set; }

    [JsonPropertyName("currentPage")] public int CurrentPage { get; set; }

    [JsonPropertyName("lastPage")] public int LastPage { get; set; }

    [JsonPropertyName("hasNextPage")] public bool HasNextPage { get; set; }

    [JsonPropertyName("perPage")] public int PerPage { get; set; }
}

public class Title
{
    [JsonPropertyName("romaji")] public string Romaji { get; set; }

    [JsonPropertyName("english")] public string English { get; set; }

    [JsonPropertyName("native")] public string Native { get; set; }

    [JsonPropertyName("userPreferred")] public string UserPreferred { get; set; }
}

//public class Date
//{
//    [JsonPropertyName("year")] public int? Year { get; set; }

//    [JsonPropertyName("month")] public int? Month { get; set; }

//    [JsonPropertyName("day")] public int? Day { get; set; }
//}

public class CoverImage
{
    [JsonPropertyName("extraLarge")] public string ExtraLarge { get; set; }

    [JsonPropertyName("large")] public string Large { get; set; }

    [JsonPropertyName("medium")] public string Medium { get; set; }

    [JsonPropertyName("color")] public string Color { get; set; }
}

public class AniListAnime
{
    private CoverImage _coverImage;

    private Title _titles;

    [JsonPropertyName("id")] public int Id { get; set; }

    [NotMapped]
    [JsonPropertyName("title")]
    public Title Titles
    {
        get => _titles;
        set
        {
            _titles = value;
            Title = TitleString;
            var titles = new List<string>(new[] { value.English, value.Native, value.Romaji });
            AlternativeTitles = string.Join(", ", titles.Where(t => !string.IsNullOrEmpty(t)));
        }
    }

    [JsonIgnore] public string Title { get; set; }
    [JsonIgnore] public string AlternativeTitles { get; set; }


    [NotMapped]
    public string TitleString => !string.IsNullOrEmpty(Titles.English) ? Titles.English :
        !string.IsNullOrEmpty(Titles.Romaji) ? Titles.Romaji : Titles.Native;

    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; }

    [NotMapped] public string DescriptionString => HtmlUtil.ConvertToPlainText(Description);

    [JsonPropertyName("siteUrl")] public string SiteUrl { get; set; }

    [NotMapped]
    [JsonPropertyName("coverImage")]
    public CoverImage CoverImage
    {
        get => _coverImage;
        set
        {
            _coverImage = value;
            Cover = value.ExtraLarge;
        }
    }

    public string Cover { get; set; }

    //[JsonPropertyName("startDate")] public Date StartDate { get; set; }

    //[JsonPropertyName("endDate")] public Date EndDate { get; set; }

    [JsonPropertyName("idMal")] public int? IdMal { get; set; }

    [JsonPropertyName("averageScore")] public int? AverageScore { get; set; }

    [JsonPropertyName("episodes")] public int? Episodes { get; set; }

    [JsonPropertyName("format")] public string Format { get; set; }

    [JsonPropertyName("season")] public string Season { get; set; }

    [JsonPropertyName("seasonYear")] public int? SeasonYear { get; set; }

    [JsonPropertyName("genres")] public string[] Genres { get; set; }

    [NotMapped] public string GenresString => Genres is not null ? string.Join(", ", Genres) : string.Empty;

    public List<Anime> Animes { get; set; }
}

public class Page
{
    [JsonPropertyName("pageInfo")] public PageInfo PageInfo { get; set; }

    [JsonPropertyName("media")] public List<AniListAnime> Media { get; set; }
}

public class AnimeSearchResult
{
    [JsonPropertyName("Page")] public Page Page { get; set; }
}