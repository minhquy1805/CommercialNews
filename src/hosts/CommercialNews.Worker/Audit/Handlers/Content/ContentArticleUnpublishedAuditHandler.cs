using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Content;

public sealed class ContentArticleUnpublishedAuditHandler
    : ContentAuditIntegrationEventHandler<ArticleUnpublishedAuditPayload>
{
    public ContentArticleUnpublishedAuditHandler(
        IContentAuditEventIngestionService ingestionService,
        ILogger<ContentArticleUnpublishedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUnpublished;

    protected override string EventDisplayName => "article unpublished";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        ContentAuditEnvelopeContext context,
        ArticleUnpublishedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleUnpublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}