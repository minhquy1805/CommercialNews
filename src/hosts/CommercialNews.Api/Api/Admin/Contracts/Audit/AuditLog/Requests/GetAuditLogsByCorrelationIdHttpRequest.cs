namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;

public sealed class GetAuditLogsByCorrelationIdHttpRequest
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
