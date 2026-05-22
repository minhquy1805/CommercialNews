using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;
using Reading.Application.Consumers.Media;
using Reading.Application.Consumers.Media.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Media;

public sealed class MediaArticleMediaDetachedReadingHandler
    : MediaReadingIntegrationEventHandler<ArticleMediaDetachedReadingPayload>
{
    public MediaArticleMediaDetachedReadingHandler(
        IMediaReadingEventIngestionService ingestionService,
        ILogger<MediaArticleMediaDetachedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticleMediaDetached;

    protected override string EventDisplayName => "article media detached";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaDetachedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleMediaDetachedAsync(
            context,
            payload,
            cancellationToken);
    }
}
