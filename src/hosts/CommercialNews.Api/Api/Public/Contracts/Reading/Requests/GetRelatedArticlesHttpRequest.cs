namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class GetRelatedArticlesHttpRequest
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public int Limit { get; init; } = 6;
}