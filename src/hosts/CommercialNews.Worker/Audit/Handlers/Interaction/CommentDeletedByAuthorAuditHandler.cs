using Audit.Application.Consumers.Interaction;
using Audit.Application.Consumers.Interaction.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Interaction;

public sealed class CommentDeletedByAuthorAuditHandler
    : InteractionAuditIntegrationEventHandler<CommentDeletedByAuthorAuditPayload>
{
    public CommentDeletedByAuthorAuditHandler(
        IInteractionAuditEventIngestionService ingestionService,
        ILogger<CommentDeletedByAuthorAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        InteractionIntegrationEventTypes.CommentDeletedByAuthor;

    protected override string EventDisplayName => "comment deleted by author";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        InteractionAuditEnvelopeContext context,
        CommentDeletedByAuthorAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestCommentDeletedByAuthorAsync(
            context,
            payload,
            cancellationToken);
    }
}
