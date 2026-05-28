using Interaction.Application.Consumers.Stats;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public sealed class InteractionArticleUnlikedStatsHandler
    : InteractionStatsIntegrationEventHandler<ArticleUnlikedPayload>
{
    public InteractionArticleUnlikedStatsHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger<InteractionArticleUnlikedStatsHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => InteractionIntegrationEventTypes.ArticleUnliked;

    protected override string EventDisplayName => "article unliked";

    protected override string GetArticlePublicId(ArticleUnlikedPayload payload)
    {
        return payload.ArticlePublicId;
    }
}
