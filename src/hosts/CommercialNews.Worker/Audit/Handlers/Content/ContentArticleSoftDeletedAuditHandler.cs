using Audit.Application.Consumers.Content;
using Audit.Application.Consumers.Content.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Content;

public sealed class ContentArticleSoftDeletedAuditHandler
    : ContentAuditIntegrationEventHandler<ArticleSoftDeletedAuditPayload>
{
    public ContentArticleSoftDeletedAuditHandler(
        IContentAuditEventIngestionService ingestionService,
        ILogger<ContentArticleSoftDeletedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleSoftDeleted;

    protected override string EventDisplayName => "article soft-deleted";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        ContentAuditEnvelopeContext context,
        ArticleSoftDeletedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleSoftDeletedAsync(
            context,
            payload,
            cancellationToken);
    }
}