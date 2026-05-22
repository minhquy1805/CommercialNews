namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class GetArticleBySlugHttpRequest
{
    public string Slug { get; init; } = string.Empty;
}