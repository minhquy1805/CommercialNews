namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Responses;

public sealed class AuditDashboardWindowHttpResponse
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}

public sealed class AuditDashboardTotalsHttpResponse
{
    public int AuditEvents { get; init; }

    public int HighRiskEvents { get; init; }

    public int CriticalEvents { get; init; }

    public int FailedIngestion { get; init; }

    public int DuplicateIngestion { get; init; }
}

public sealed class AuditDashboardCountByModuleHttpResponse
{
    public string SourceModule { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class AuditDashboardCountBySeverityHttpResponse
{
    public string Severity { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class AuditDashboardCountByRiskLevelHttpResponse
{
    public string RiskLevel { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class AuditDashboardFreshnessHttpResponse
{
    public DateTime GeneratedAtUtc { get; init; }

    public int? OldestFailedIngestionAgeSeconds { get; init; }
}

public sealed class GetAuditDashboardSummaryHttpResponse
{
    public AuditDashboardWindowHttpResponse Window { get; init; } = new();

    public AuditDashboardTotalsHttpResponse Totals { get; init; } = new();

    public IReadOnlyList<AuditDashboardCountByModuleHttpResponse> ByModule { get; init; } =
        Array.Empty<AuditDashboardCountByModuleHttpResponse>();

    public IReadOnlyList<AuditDashboardCountBySeverityHttpResponse> BySeverity { get; init; } =
        Array.Empty<AuditDashboardCountBySeverityHttpResponse>();

    public IReadOnlyList<AuditDashboardCountByRiskLevelHttpResponse> ByRiskLevel { get; init; } =
        Array.Empty<AuditDashboardCountByRiskLevelHttpResponse>();

    public AuditDashboardFreshnessHttpResponse Freshness { get; init; } = new();
}
