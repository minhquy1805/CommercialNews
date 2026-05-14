using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Content;

public sealed class ContentArticleCreatedAuditHandler
    : ContentAuditIntegrationEventHandler<ArticleCreatedAuditPayload>
{
    public ContentArticleCreatedAuditHandler(
        IContentAuditEventIngestionService ingestionService,
        ILogger<ContentArticleCreatedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleCreated;

    protected override string EventDisplayName => "article created";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        ContentAuditEnvelopeContext context,
        ArticleCreatedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleCreatedAsync(
            context,
            payload,
            cancellationToken);
    }
}