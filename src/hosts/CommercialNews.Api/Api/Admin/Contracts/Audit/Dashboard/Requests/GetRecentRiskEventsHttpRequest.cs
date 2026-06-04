namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Requests;

public sealed class GetRecentRiskEventsHttpRequest
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? SourceModule { get; init; }

    public string? RiskLevel { get; init; }

    public int Limit { get; init; } = 20;
}
