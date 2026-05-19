namespace Media.Application.Models.Queries;

public sealed class ArticleMediaListQuery
{
    public long ArticleId { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public bool IncludeDeleted { get; init; }

    public string SortBy { get; init; } = "SortOrder";
    public string SortDirection { get; init; } = "ASC";

    public int Skip => Page <= 1 ? 0 : (Page - 1) * PageSize;
    public int Take => PageSize <= 0 ? 20 : PageSize;
}