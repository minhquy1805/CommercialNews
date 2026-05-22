using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Reading.Application.Consumers.Content;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Content;

public sealed class ContentArticleUnpublishedReadingHandler
    : ContentReadingIntegrationEventHandler<ArticleUnpublishedReadingPayload>
{
    public ContentArticleUnpublishedReadingHandler(
        IContentReadingEventIngestionService ingestionService,
        ILogger<ContentArticleUnpublishedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleUnpublished;

    protected override string EventDisplayName => "article unpublished";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        ContentReadingEnvelopeContext context,
        ArticleUnpublishedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleUnpublishedAsync(
            context,
            payload,
            cancellationToken);
    }
}