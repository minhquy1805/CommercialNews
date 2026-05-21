namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class GetRelatedArticlesHttpResponse
{
    public IReadOnlyList<ArticleListItemHttpResponse> Items { get; init; } = [];
}