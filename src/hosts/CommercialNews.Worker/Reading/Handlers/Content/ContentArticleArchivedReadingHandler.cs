using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Outbox;
using Reading.Application.Consumers.Content;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Content;

public sealed class ContentArticleArchivedReadingHandler
    : ContentReadingIntegrationEventHandler<ArticleArchivedReadingPayload>
{
    public ContentArticleArchivedReadingHandler(
        IContentReadingEventIngestionService ingestionService,
        ILogger<ContentArticleArchivedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => ContentIntegrationEventTypes.ArticleArchived;

    protected override string EventDisplayName => "article archived";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        ContentReadingEnvelopeContext context,
        ArticleArchivedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleArchivedAsync(
            context,
            payload,
            cancellationToken);
    }
}