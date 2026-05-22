namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class SearchArticlesHttpRequest
{
    public string Query { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Sort { get; init; }
}