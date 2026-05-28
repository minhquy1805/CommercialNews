using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Interaction.Application.Consumers.Content;
using Interaction.Application.Consumers.Content.Payloads;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace CommercialNews.Worker.Interaction.Handlers.Content;

public sealed class ContentArticleUnpublishedInteractionHandler
    : ContentInteractionIntegrationEventHandler<ArticleUnpublishedInteractionPayload>
{
    public ContentArticleUnpublishedInteractionHandler(
        IContentInteractionEventIngestionService ingestionService,
        ILogger<ContentArticleUnpublishedInteractionHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUnpublished;

    protected override string EventDisplayName => "article unpublished";

    protected override Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> IngestAsync(
        ContentInteractionEnvelopeContext context,
        ArticleUnpublishedInteractionPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleUnpublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}
