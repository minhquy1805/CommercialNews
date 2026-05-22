namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class GetArticlesHttpResponse
{
    public IReadOnlyList<ArticleListItemHttpResponse> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalItems { get; init; }

    public int TotalPages { get; init; }
}