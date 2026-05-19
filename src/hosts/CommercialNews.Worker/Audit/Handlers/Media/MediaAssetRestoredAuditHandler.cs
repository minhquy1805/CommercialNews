using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class MediaAssetRestoredAuditHandler
    : MediaAuditIntegrationEventHandler<MediaAssetRestoredAuditPayload>
{
    public MediaAssetRestoredAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<MediaAssetRestoredAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.AssetRestored;

    protected override string EventDisplayName => "asset restored";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetRestoredAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestMediaAssetRestoredAsync(
            context,
            payload,
            cancellationToken);
    }
}