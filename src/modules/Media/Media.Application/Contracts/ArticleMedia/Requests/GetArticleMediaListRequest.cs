namespace Media.Application.Contracts.ArticleMedia.Requests;

public sealed class GetArticleMediaListRequest
{
    public long ArticleId { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public bool IncludeDeleted { get; init; }

    public string SortBy { get; init; } = "SortOrder";
    public string SortDirection { get; init; } = "ASC";
}