using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Consumers.Media;

public interface IMediaAuditEventIngestionService
{
    Task<Result<AuditIngestionResult>> IngestMediaAssetRegisteredAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetRegisteredAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestMediaAssetUpdatedAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetUpdatedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestMediaAssetSoftDeletedAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetSoftDeletedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestMediaAssetRestoredAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetRestoredAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleMediaAttachedAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaAttachedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleMediaDetachedAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaDetachedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticleMediaReorderedAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaReorderedAuditPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<AuditIngestionResult>> IngestArticlePrimaryMediaSetAsync(
        MediaAuditEnvelopeContext context,
        ArticlePrimaryMediaSetAuditPayload payload,
        CancellationToken cancellationToken = default);
}