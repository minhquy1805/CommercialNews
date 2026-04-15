using Audit.Application.Models;
using Audit.Application.Models.QueryModels;
using Audit.Domain.Entities;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace Audit.Application.Ports.Persistence;

public interface IAuditLogRepository
{
    Task<AuditInsertResult> InsertIfNotExistsAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByIdAsync(
        long auditId,
        CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByAuditEventIdAsync(
        string auditEventId,
        CancellationToken cancellationToken = default);

    Task<AuditLogDetailResult?> SelectDetailByIdAsync(
        long auditId,
        CancellationToken cancellationToken = default);

    Task<AuditLogDetailResult?> SelectDetailByAuditEventIdAsync(
        string auditEventId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditLogListResultItem>> SelectSkipAndTakeAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditLogListResultItem>> SelectByCorrelationIdAsync(
        AuditLogByCorrelationQuery query,
        CancellationToken cancellationToken = default);
}