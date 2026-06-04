using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace CommercialNews.Api.Api.Common.Contracts;

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public PageInfo PageInfo { get; init; } = new();

    public static PagedResponse<T> From<TSource>(
        PagedQueryResult<TSource> result,
        Func<TSource, T> mapItem)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapItem);

        return new PagedResponse<T>
        {
            Items = result.Items
                .Select(mapItem)
                .ToArray(),
            PageInfo = new PageInfo
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.PageSize <= 0
                    ? 0
                    : (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)
            }
        };
    }
}
