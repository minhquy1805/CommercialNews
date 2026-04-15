namespace CommercialNews.BuildingBlocks.SharedKernel.Paging;

public sealed class PagedQueryResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}