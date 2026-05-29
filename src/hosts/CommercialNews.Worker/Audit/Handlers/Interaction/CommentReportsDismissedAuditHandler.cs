using Audit.Application.Consumers.Interaction;
using Audit.Application.Consumers.Interaction.Payloads;
using Audit.Application.Contracts.Ingestion;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Outbox;

namespace CommercialNews.Worker.Audit.Handlers.Interaction;

public sealed class CommentReportsDismissedAuditHandler
    : InteractionAuditIntegrationEventHandler<CommentReportsDismissedAuditPayload>
{
    public CommentReportsDismissedAuditHandler(
        IInteractionAuditEventIngestionService ingestionService,
        ILogger<CommentReportsDismissedAuditHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        InteractionIntegrationEventTypes.CommentReportsDismissed;

    protected override string EventDisplayName => "comment reports dismissed";

    protected override Task<Result<AuditIngestionResult>> IngestAsync(
        InteractionAuditEnvelopeContext context,
        CommentReportsDismissedAuditPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestCommentReportsDismissedAsync(
            context,
            payload,
            cancellationToken);
    }
}