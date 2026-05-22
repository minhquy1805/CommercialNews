namespace Reading.Application.Contracts.Articles.Responses;

public sealed class GetArticlesResponse
{
    public IReadOnlyList<ArticleListItemResponse> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public long TotalItems { get; set; }

    public int TotalPages { get; set; }
}