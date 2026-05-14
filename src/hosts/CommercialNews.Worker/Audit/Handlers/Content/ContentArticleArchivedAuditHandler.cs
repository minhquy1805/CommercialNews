using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Content;

public sealed class ContentArticleArchivedAuditHandler
    : ContentAuditIntegrationEventHandler<ArticleArchivedAuditPayload>
{
    public ContentArticleArchivedAuditHandler(
        IContentAuditEventIngestionService ingestionService,
        ILogger<ContentArticleArchivedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleArchived;

    protected override string EventDisplayName => "article archived";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        ContentAuditEnvelopeContext context,
        ArticleArchivedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleArchivedAsync(
            context,
            payload,
            cancellationToken);
    }
}