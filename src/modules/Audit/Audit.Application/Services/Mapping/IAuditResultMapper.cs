using Audit.Application.Abstractions.Persistence.Results;
using Audit.Application.Models.Results.AuditLogs;
using Audit.Application.Models.Results.Dashboard;
using Audit.Application.Models.Results.Ingestion;
using Audit.Domain.Entities;

namespace Audit.Application.Services.Mapping;

public interface IAuditResultMapper
{
    AuditLogListItemResult ToAuditLogListItem(
        AuditLog auditLog);

    AuditLogDetailResult ToAuditLogDetail(
        AuditLog auditLog);

    AuditIngestionListItemResult ToAuditIngestionListItem(
        AuditIngestion ingestion);

    AuditIngestionDetailResult ToAuditIngestionDetail(
        AuditIngestion ingestion);

    RecentRiskAuditEventResult ToRecentRiskEvent(
        AuditLog auditLog);

    AuditDashboardCountByModuleResult ToModuleCount(
        AuditCountByValueResult result);

    AuditDashboardCountBySeverityResult ToSeverityCount(
        AuditCountByValueResult result);

    AuditDashboardCountByRiskLevelResult ToRiskLevelCount(
        AuditCountByValueResult result);

    AuditDashboardSummaryResult ToDashboardSummary(
        AuditDashboardSummaryDataResult data);
}