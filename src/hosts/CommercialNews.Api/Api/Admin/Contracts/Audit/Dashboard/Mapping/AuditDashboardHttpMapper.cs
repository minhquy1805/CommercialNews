using Audit.Application.Models.Results.Dashboard;
using CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Mapping;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Responses;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Dashboard.Mapping;

internal static class AuditDashboardHttpMapper
{
    public static GetAuditDashboardSummaryHttpResponse ToSummary(
        AuditDashboardSummaryResult result)
    {
        return new GetAuditDashboardSummaryHttpResponse
        {
            Window = new AuditDashboardWindowHttpResponse
            {
                FromUtc = result.Window.FromUtc,
                ToUtc = result.Window.ToUtc
            },
            Totals = new AuditDashboardTotalsHttpResponse
            {
                AuditEvents = result.Totals.AuditEvents,
                HighRiskEvents = result.Totals.HighRiskEvents,
                CriticalEvents = result.Totals.CriticalEvents,
                FailedIngestion = result.Totals.FailedIngestion,
                DuplicateIngestion = result.Totals.DuplicateIngestion
            },
            ByModule = result.ByModule
                .Select(static item => new AuditDashboardCountByModuleHttpResponse
                {
                    SourceModule = item.SourceModule,
                    Count = item.Count
                })
                .ToArray(),
            BySeverity = result.BySeverity
                .Select(static item => new AuditDashboardCountBySeverityHttpResponse
                {
                    Severity = item.Severity,
                    Count = item.Count
                })
                .ToArray(),
            ByRiskLevel = result.ByRiskLevel
                .Select(static item => new AuditDashboardCountByRiskLevelHttpResponse
                {
                    RiskLevel = item.RiskLevel,
                    Count = item.Count
                })
                .ToArray(),
            Freshness = new AuditDashboardFreshnessHttpResponse
            {
                GeneratedAtUtc = result.Freshness.GeneratedAtUtc,
                OldestFailedIngestionAgeSeconds = result.Freshness.OldestFailedIngestionAgeSeconds
            }
        };
    }

    public static GetRecentRiskEventsHttpResponse ToRecentRiskEvents(
        IReadOnlyList<RecentRiskAuditEventResult> result)
    {
        return new GetRecentRiskEventsHttpResponse
        {
            Items = result
                .Select(AuditLogHttpMapper.ToListItem)
                .ToArray()
        };
    }
}
