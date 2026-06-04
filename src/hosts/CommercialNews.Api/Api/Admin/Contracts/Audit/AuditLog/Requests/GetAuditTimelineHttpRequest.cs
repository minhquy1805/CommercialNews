namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;

public sealed class GetAuditTimelineHttpRequest
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? SourceModule { get; init; }

    public string? RiskLevel { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Sort { get; init; }
}
