namespace Reading.Application.Contracts.Articles.Responses;

public sealed class GetArticlesResponse
{
    public IReadOnlyList<ArticleListItemResponse> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }
}