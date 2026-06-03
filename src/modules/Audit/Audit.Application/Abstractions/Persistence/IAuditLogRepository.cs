using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Abstractions.Persistence.Results;
using Audit.Domain.Entities;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace Audit.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task<AuditLogInsertResult> InsertAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditLog>> SearchAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditLog>> GetByCorrelationIdAsync(
        string correlationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditLog>> GetResourceTimelineAsync(
        string resourceType,
        string resourceId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditLog>> GetActorTimelineAsync(
        string actorUserId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        AuditLogSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetRecentRiskEventsAsync(
        AuditRecentRiskEventsSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountHighRiskAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountCriticalAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditCountByValueResult>> CountByModuleAsync(
    AuditDashboardSummarySearchQuery query,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditCountByValueResult>> CountBySeverityAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditCountByValueResult>> CountByRiskLevelAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default);
}