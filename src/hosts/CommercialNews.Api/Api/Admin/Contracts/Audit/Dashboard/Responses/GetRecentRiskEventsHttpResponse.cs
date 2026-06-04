using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Responses;

public sealed class GetRecentRiskEventsHttpResponse
{
    public IReadOnlyList<AuditLogListItemHttpResponse> Items { get; init; } =
        Array.Empty<AuditLogListItemHttpResponse>();
}
