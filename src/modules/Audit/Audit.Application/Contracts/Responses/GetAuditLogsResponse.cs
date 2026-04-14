namespace Audit.Application.Contracts.Responses;

public sealed class GetAuditLogsResponse
{
    public IReadOnlyList<AuditLogListItemResponse> Items { get; init; } = Array.Empty<AuditLogListItemResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalCount { get; init; }

    public int TotalPages { get; init; }
}