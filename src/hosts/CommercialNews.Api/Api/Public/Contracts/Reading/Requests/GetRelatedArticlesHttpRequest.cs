namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class GetRelatedArticlesHttpRequest
{
    public int Limit { get; init; } = 6;
}