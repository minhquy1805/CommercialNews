using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class MediaAssetSoftDeletedAuditHandler
    : MediaAuditIntegrationEventHandler<MediaAssetSoftDeletedAuditPayload>
{
    public MediaAssetSoftDeletedAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<MediaAssetSoftDeletedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.AssetSoftDeleted;

    protected override string EventDisplayName => "asset soft-deleted";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetSoftDeletedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestMediaAssetSoftDeletedAsync(
            context,
            payload,
            cancellationToken);
    }
}