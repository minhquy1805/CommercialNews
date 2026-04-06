namespace CommercialNews.Api.Api.Public.Contracts.Reading.Requests;

public sealed class GetArticlesHttpRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public long? CategoryId { get; init; }

    public long? TagId { get; init; }

    public string? Q { get; init; }

    public string? Sort { get; init; }
}