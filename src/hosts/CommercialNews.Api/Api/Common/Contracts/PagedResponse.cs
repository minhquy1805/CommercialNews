using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace CommercialNews.Api.Api.Common.Contracts;

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public PageInfo PageInfo { get; init; } = new();
}