using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Content;

public interface IContentAuditEventIngestionService
{
    Task<Result<AuditIngestionResult>> IngestArticleCreatedAsync(
        ContentAuditEnvelopeContext context,
        ArticleCreatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleUpdatedAsync(
        ContentAuditEnvelopeContext context,
        ArticleUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticlePublishedAsync(
        ContentAuditEnvelopeContext context,
        ArticlePublishedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleUnpublishedAsync(
        ContentAuditEnvelopeContext context,
        ArticleUnpublishedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleArchivedAsync(
        ContentAuditEnvelopeContext context,
        ArticleArchivedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleSoftDeletedAsync(
        ContentAuditEnvelopeContext context,
        ArticleSoftDeletedAuditPayload payload,
        CancellationToken cancellationToken = default);
}