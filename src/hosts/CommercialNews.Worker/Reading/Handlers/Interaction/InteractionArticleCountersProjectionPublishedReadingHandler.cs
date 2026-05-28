using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Outbox;
using Reading.Application.Consumers.Interaction;
using Reading.Application.Consumers.Interaction.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Interaction;

public sealed class InteractionArticleCountersProjectionPublishedReadingHandler
    : InteractionReadingIntegrationEventHandler<
        ArticleCountersProjectionPublishedReadingPayload>
{
    public InteractionArticleCountersProjectionPublishedReadingHandler(
        IInteractionReadingEventIngestionService ingestionService,
        ILogger<InteractionArticleCountersProjectionPublishedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType =>
        InteractionIntegrationEventTypes.ArticleCountersProjectionPublished;

    protected override string EventDisplayName =>
        "article counters projection published";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        InteractionReadingEnvelopeContext context,
        ArticleCountersProjectionPublishedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleCountersProjectionPublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}
