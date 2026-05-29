using Interaction.Application.Consumers.Stats;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public sealed class InteractionCommentHiddenStatsHandler
    : InteractionStatsIntegrationEventHandler<CommentHiddenPayload>
{
    public InteractionCommentHiddenStatsHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger<InteractionCommentHiddenStatsHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => InteractionIntegrationEventTypes.CommentHidden;

    protected override string EventDisplayName => "comment hidden";

    protected override string GetArticlePublicId(CommentHiddenPayload payload)
    {
        return payload.ArticlePublicId;
    }
}
