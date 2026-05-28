using Interaction.Application.Consumers.Stats;
using Interaction.Application.Outbox;
using Interaction.Application.Outbox.Payloads;

namespace CommercialNews.Worker.Interaction.Handlers.Stats;

public sealed class InteractionCommentDeletedByAuthorStatsHandler
    : InteractionStatsIntegrationEventHandler<CommentDeletedByAuthorPayload>
{
    public InteractionCommentDeletedByAuthorStatsHandler(
        IInteractionStatsEventIngestionService ingestionService,
        ILogger<InteractionCommentDeletedByAuthorStatsHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        InteractionIntegrationEventTypes.CommentDeletedByAuthor;

    protected override string EventDisplayName => "comment deleted by author";

    protected override string GetArticlePublicId(
        CommentDeletedByAuthorPayload payload)
    {
        return payload.ArticlePublicId;
    }
}
