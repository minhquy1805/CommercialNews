using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class MediaAssetRegisteredAuditHandler
    : MediaAuditIntegrationEventHandler<MediaAssetRegisteredAuditPayload>
{
    public MediaAssetRegisteredAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<MediaAssetRegisteredAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.AssetRegistered;

    protected override string EventDisplayName => "asset registered";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        MediaAssetRegisteredAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestMediaAssetRegisteredAsync(
            context,
            payload,
            cancellationToken);
    }
}