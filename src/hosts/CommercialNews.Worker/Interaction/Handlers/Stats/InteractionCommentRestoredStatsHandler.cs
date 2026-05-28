using Interaction.Application.Consumers.Stats;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public sealed class InteractionCommentRestoredStatsHandler
    : InteractionStatsIntegrationEventHandler<CommentRestoredPayload>
{
    public InteractionCommentRestoredStatsHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger<InteractionCommentRestoredStatsHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => InteractionIntegrationEventTypes.CommentRestored;

    protected override string EventDisplayName => "comment restored";

    protected override string GetArticlePublicId(CommentRestoredPayload payload)
    {
        return payload.ArticlePublicId;
    }
}
