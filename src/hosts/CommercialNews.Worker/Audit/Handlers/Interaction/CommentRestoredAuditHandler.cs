using Audit.Application.Consumers.Interaction;
using Audit.Application.Consumers.Interaction.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Interaction;

public sealed class CommentRestoredAuditHandler
    : InteractionAuditIntegrationEventHandler<CommentRestoredAuditPayload>
{
    public CommentRestoredAuditHandler(
        IInteractionAuditEventIngestionService ingestionService,
        ILogger<CommentRestoredAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        InteractionIntegrationEventTypes.CommentRestored;

    protected override string EventDisplayName => "comment restored";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        InteractionAuditEnvelopeContext context,
        CommentRestoredAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestCommentRestoredAsync(
            context,
            payload,
            cancellationToken);
    }
}