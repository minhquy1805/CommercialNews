using Audit.Application.Consumers.Interaction;
using Audit.Application.Consumers.Interaction.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Interaction;

public sealed class CommentHiddenAuditHandler
    : InteractionAuditIntegrationEventHandler<CommentHiddenAuditPayload>
{
    public CommentHiddenAuditHandler(
        IInteractionAuditEventIngestionService ingestionService,
        ILogger<CommentHiddenAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        InteractionIntegrationEventTypes.CommentHidden;

    protected override string EventDisplayName => "comment hidden";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        InteractionAuditEnvelopeContext context,
        CommentHiddenAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestCommentHiddenAsync(
            context,
            payload,
            cancellationToken);
    }
}