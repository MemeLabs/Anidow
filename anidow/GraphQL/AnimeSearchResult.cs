using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anidow.GraphQL
{
    public class PageInfo
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("lastPage")]
        public int LastPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("perPage")]
        public int PerPage { get; set; }
    }

    public class Title
    {
        [JsonPropertyName("romaji")]
        public string Romaji { get; set; }

        [JsonPropertyName("english")]
        public string English { get; set; }

        [JsonPropertyName("native")]
        public string Native { get; set; }

        [JsonPropertyName("userPreferred")]
        public string UserPreferred { get; set; }
    }

    public class CoverImage
    {
        [JsonPropertyName("extraLarge")]
        public string ExtraLarge { get; set; }

        [JsonPropertyName("large")]
        public string Large { get; set; }

        [JsonPropertyName("medium")]
        public string Medium { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }
    }

    public class Medium
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public Title Title { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("siteUrl")]
        public string SiteUrl { get; set; }

        [JsonPropertyName("coverImage")]
        public CoverImage CoverImage { get; set; }
    }

    public class Page
    {
        [JsonPropertyName("pageInfo")]
        public PageInfo PageInfo { get; set; }

        [JsonPropertyName("media")]
        public List<Medium> Media { get; set; }
    }

    public class AnimeSearchResult
    {
        [JsonPropertyName("Page")]
        public Page Page { get; set; }
    }
}