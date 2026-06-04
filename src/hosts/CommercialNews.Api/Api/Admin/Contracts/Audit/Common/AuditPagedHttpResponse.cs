using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

public class AuditPagedHttpResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public PageInfo PageInfo { get; init; } = new();
}
