namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class GetArticleBySlugHttpRequest
{
    public string Scope { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;
}