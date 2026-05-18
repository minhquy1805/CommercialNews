using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class MediaAssetUpdatedAuditHandler
    : MediaAuditIntegrationEventHandler<MediaAssetUpdatedAuditPayload>
{
    public MediaAssetUpdatedAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<MediaAssetUpdatedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.AssetUpdated;

    protected override string EventDisplayName => "asset updated";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetUpdatedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestMediaAssetUpdatedAsync(
            context,
            payload,
            cancellationToken);
    }
}