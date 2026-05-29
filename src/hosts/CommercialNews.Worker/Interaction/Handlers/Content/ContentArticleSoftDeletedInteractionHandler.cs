using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Interaction.Application.Consumers.Content;
using Interaction.Application.Consumers.Content.Payloads;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace CommercialNews.Worker.Interaction.Handlers.Content;

public sealed class ContentArticleSoftDeletedInteractionHandler
    : ContentInteractionIntegrationEventHandler<ArticleSoftDeletedInteractionPayload>
{
    public ContentArticleSoftDeletedInteractionHandler(
        IContentInteractionEventIngestionService ingestionService,
        ILogger<ContentArticleSoftDeletedInteractionHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleSoftDeleted;

    protected override string EventDisplayName => "article soft deleted";

    protected override Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> IngestAsync(
        ContentInteractionEnvelopeContext context,
        ArticleSoftDeletedInteractionPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleSoftDeletedAsync(
            context,
            payload,
            cancellationToken);
    }
}
