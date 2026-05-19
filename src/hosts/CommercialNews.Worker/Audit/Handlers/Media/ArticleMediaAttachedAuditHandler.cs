using Audit.Application.Consumers.Media;
using Audit.Application.Consumers.Media.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Media;

public sealed class ArticleMediaAttachedAuditHandler
    : MediaAuditIntegrationEventHandler<ArticleMediaAttachedAuditPayload>
{
    public ArticleMediaAttachedAuditHandler(
        IMediaAuditEventIngestionService ingestionService,
        ILogger<ArticleMediaAttachedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticleMediaAttached;

    protected override string EventDisplayName => "article media attached";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        MediaAuditEnvelopeContext context,
        ArticleMediaAttachedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleMediaAttachedAsync(
            context,
            payload,
            cancellationToken);
    }
}