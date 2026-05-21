namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class ArticleTagHttpResponse
{
    public long TagId { get; init; }

    public string? TagPublicId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }
}