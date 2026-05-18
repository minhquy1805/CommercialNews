using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class ArticlePrimaryMediaSetAuditHandler
    : MediaAuditIntegrationEventHandler<ArticlePrimaryMediaSetAuditPayload>
{
    public ArticlePrimaryMediaSetAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<ArticlePrimaryMediaSetAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticlePrimaryMediaSet;

    protected override string EventDisplayName => "article primary media set";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        ArticlePrimaryMediaSetAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticlePrimaryMediaSetAsync(
            context,
            payload,
            cancellationToken);
    }
}