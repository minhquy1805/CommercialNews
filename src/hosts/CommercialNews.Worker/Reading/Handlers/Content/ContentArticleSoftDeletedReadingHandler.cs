using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Reading.Application.Consumers.Content;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Content;

public sealed class ContentArticleSoftDeletedReadingHandler
    : ContentReadingIntegrationEventHandler<ArticleSoftDeletedReadingPayload>
{
    public ContentArticleSoftDeletedReadingHandler(
        IContentReadingEventIngestionService ingestionService,
        ILogger<ContentArticleSoftDeletedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleSoftDeleted;

    protected override string EventDisplayName => "article soft deleted";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        ContentReadingEnvelopeContext context,
        ArticleSoftDeletedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleSoftDeletedAsync(
            context,
            payload,
            cancellationToken);
    }
}