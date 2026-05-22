using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Reading.Application.Consumers.Content;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Content;

public sealed class ContentArticleUpdatedReadingHandler
    : ContentReadingIntegrationEventHandler<ArticleUpdatedReadingPayload>
{
    public ContentArticleUpdatedReadingHandler(
        IContentReadingEventIngestionService ingestionService,
        ILogger<ContentArticleUpdatedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUpdated;

    protected override string EventDisplayName => "article updated";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        ContentReadingEnvelopeContext context,
        ArticleUpdatedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleUpdatedAsync(
            context,
            payload,
            cancellationToken);
    }
}