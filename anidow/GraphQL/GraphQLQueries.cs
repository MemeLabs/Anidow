using GraphQL;

namespace Anidow.GraphQL;

public class GraphQLQueries
{
    public static GraphQLRequest SearchQuery(string search, int results = 5, int page = 1) =>
        new GraphQLRequest
        {
            Query = @"
                    query AnimeSearch($search: String, $perPage: Int) {
                        Page(perPage: $perPage, page: 1) {
                            pageInfo {
                                total
                                currentPage
                                lastPage
                                hasNextPage
                                perPage
                            }
                            media(search: $search, type: ANIME) {
                            id
                            title {
                                romaji
                                english
                                native
                                userPreferred
                            }
                            status
                            description
                            siteUrl
                            coverImage {
                                extraLarge
                                large
                                medium
                                color
                            }
                            idMal
                            genres
                            averageScore
                            episodes
                            season
                            seasonYear
                            format
                        }
                    }
                }",
            OperationName = "AnimeSearch",
            Variables = new
            {
                search,
                page,
                perPage = results,
            },
        };
}