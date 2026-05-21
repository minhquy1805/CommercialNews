using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Reading.Application.Consumers.Content;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Content;

public sealed class ContentArticlePublishedReadingHandler
    : ContentReadingIntegrationEventHandler<ArticlePublishedReadingPayload>
{
    public ContentArticlePublishedReadingHandler(
        IContentReadingEventIngestionService ingestionService,
        ILogger<ContentArticlePublishedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticlePublished;

    protected override string EventDisplayName => "article published";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        ContentReadingEnvelopeContext context,
        ArticlePublishedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticlePublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}