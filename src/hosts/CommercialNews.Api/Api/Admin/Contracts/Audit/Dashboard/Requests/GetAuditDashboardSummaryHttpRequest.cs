namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Requests;

public sealed class GetAuditDashboardSummaryHttpRequest
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? SourceModule { get; init; }
}
