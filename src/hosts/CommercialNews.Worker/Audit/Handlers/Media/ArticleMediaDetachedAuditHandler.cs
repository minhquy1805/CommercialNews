using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class ArticleMediaDetachedAuditHandler
    : MediaAuditIntegrationEventHandler<ArticleMediaDetachedAuditPayload>
{
    public ArticleMediaDetachedAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<ArticleMediaDetachedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticleMediaDetached;

    protected override string EventDisplayName => "article media detached";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaDetachedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleMediaDetachedAsync(
            context,
            payload,
            cancellationToken);
    }
}