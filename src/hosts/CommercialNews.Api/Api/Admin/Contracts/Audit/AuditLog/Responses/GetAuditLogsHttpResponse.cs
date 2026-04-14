namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;

public sealed class GetAuditLogsHttpResponse
{
    public IReadOnlyList<AuditLogListItemHttpResponse> Items { get; init; } = Array.Empty<AuditLogListItemHttpResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}