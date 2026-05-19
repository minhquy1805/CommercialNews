namespace Media.Application.Models.Queries;

public sealed class MediaAssetListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public bool? IsDeleted { get; init; }
    public string? MediaType { get; init; }

    public string SortBy { get; init; } = "CreatedAt";
    public string SortDirection { get; init; } = "DESC";

    public int Skip => Page <= 1 ? 0 : (Page - 1) * PageSize;
    public int Take => PageSize <= 0 ? 20 : PageSize;
}