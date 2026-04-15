namespace CommercialNews.BuildingBlocks.SharedKernel.Paging;

public sealed class PageInfo
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
}