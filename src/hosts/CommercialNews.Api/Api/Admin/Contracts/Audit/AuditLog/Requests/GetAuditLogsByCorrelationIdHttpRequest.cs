namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;

public sealed class GetAuditLogsByCorrelationIdHttpRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}