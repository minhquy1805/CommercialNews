using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Content;

public sealed class ContentArticleUpdatedAuditHandler
    : ContentAuditIntegrationEventHandler<ArticleUpdatedAuditPayload>
{
    public ContentArticleUpdatedAuditHandler(
        IContentAuditEventIngestionService ingestionService,
        ILogger<ContentArticleUpdatedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUpdated;

    protected override string EventDisplayName => "article updated";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        ContentAuditEnvelopeContext context,
        ArticleUpdatedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleUpdatedAsync(
            context,
            payload,
            cancellationToken);
    }
}