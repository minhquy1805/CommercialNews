using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Interaction.Application.Consumers.Content;
using Interaction.Application.Consumers.Content.Payloads;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace CommercialNews.Worker.Interaction.Handlers.Content;

public sealed class ContentArticleArchivedInteractionHandler
    : ContentInteractionIntegrationEventHandler<ArticleArchivedInteractionPayload>
{
    public ContentArticleArchivedInteractionHandler(
        IContentInteractionEventIngestionService ingestionService,
        ILogger<ContentArticleArchivedInteractionHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleArchived;

    protected override string EventDisplayName => "article archived";

    protected override Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>> IngestAsync(
        ContentInteractionEnvelopeContext context,
        ArticleArchivedInteractionPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleArchivedAsync(
            context,
            payload,
            cancellationToken);
    }
}
