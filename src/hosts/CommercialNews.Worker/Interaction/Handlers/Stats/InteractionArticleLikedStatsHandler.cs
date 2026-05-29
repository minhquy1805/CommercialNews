using Interaction.Application.Consumers.Stats;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public sealed class InteractionArticleLikedStatsHandler
    : InteractionStatsIntegrationEventHandler<ArticleLikedPayload>
{
    public InteractionArticleLikedStatsHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger<InteractionArticleLikedStatsHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => InteractionIntegrationEventTypes.ArticleLiked;

    protected override string EventDisplayName => "article liked";

    protected override string GetArticlePublicId(ArticleLikedPayload payload)
    {
        return payload.ArticlePublicId;
    }
}
