using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Interaction.Application.Consumers.Content;
using Interaction.Application.Consumers.Content.Payloads;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace CommercialNews.Worker.Interaction.Handlers.Content;

public sealed class ContentArticlePublishedInteractionHandler
    : ContentInteractionIntegrationEventHandler<ArticlePublishedInteractionPayload>
{
    public ContentArticlePublishedInteractionHandler(
        IContentInteractionEventIngestionService ingestionService,
        ILogger<ContentArticlePublishedInteractionHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticlePublished;

    protected override string EventDisplayName => "article published";

    protected override Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> IngestAsync(
        ContentInteractionEnvelopeContext context,
        ArticlePublishedInteractionPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticlePublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}
