using Interaction.Application.Consumers.Stats;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public sealed class InteractionCommentCreatedStatsHandler
    : InteractionStatsIntegrationEventHandler<CommentCreatedPayload>
{
    public InteractionCommentCreatedStatsHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger<InteractionCommentCreatedStatsHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => InteractionIntegrationEventTypes.CommentCreated;

    protected override string EventDisplayName => "comment created";

    protected override string GetArticlePublicId(CommentCreatedPayload payload)
    {
        return payload.ArticlePublicId;
    }
}
