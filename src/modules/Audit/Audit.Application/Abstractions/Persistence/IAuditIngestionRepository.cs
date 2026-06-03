using Audit.Application.Abstractions.Persistence.Queries;
using Audit.Application.Abstractions.Persistence.Results;
using Audit.Domain.Entities;
using Audit.Domain.ValueObjects.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace Audit.Application.Abstractions.Persistence;

public interface IAuditIngestionRepository
{
    Task<AuditIngestionUpsertResult> UpsertProcessingAsync(
        AuditIngestion auditIngestion,
        CancellationToken cancellationToken = default);

    Task MarkSucceededAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task MarkDuplicateAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task MarkIgnoredAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        string messageId,
        AuditErrorInfo errorInfo,
        CancellationToken cancellationToken = default);

    Task MarkDeadLetteredAsync(
        string messageId,
        AuditErrorInfo errorInfo,
        CancellationToken cancellationToken = default);

    Task<AuditIngestion?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    Task<AuditIngestion?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditIngestion>> SearchAsync(
        AuditIngestionSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<AuditIngestion>> SearchFailedAsync(
        AuditFailedIngestionSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        AuditIngestionSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountFailedAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountDuplicateAsync(
        AuditDashboardSummarySearchQuery query,
        CancellationToken cancellationToken = default);

    Task<int?> GetOldestFailedIngestionAgeSecondsAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}