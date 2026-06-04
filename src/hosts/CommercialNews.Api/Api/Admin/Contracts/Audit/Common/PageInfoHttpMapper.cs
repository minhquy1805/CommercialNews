using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

internal static class PageInfoHttpMapper
{
    public static PageInfo ToPageInfo<T>(
        PagedQueryResult<T> result)
    {
        return new PageInfo
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.PageSize <= 0
                ? 0
                : (int)Math.Ceiling(result.TotalItems / (double)result.PageSize)
        };
    }
}
