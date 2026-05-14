using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Content;

public sealed class ContentArticlePublishedAuditHandler
    : ContentAuditIntegrationEventHandler<ArticlePublishedAuditPayload>
{
    public ContentArticlePublishedAuditHandler(
        IContentAuditEventIngestionService ingestionService,
        ILogger<ContentArticlePublishedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticlePublished;

    protected override string EventDisplayName => "article published";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        ContentAuditEnvelopeContext context,
        ArticlePublishedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticlePublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}