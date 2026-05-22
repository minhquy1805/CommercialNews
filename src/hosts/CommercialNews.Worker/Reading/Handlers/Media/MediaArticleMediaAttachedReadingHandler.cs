using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;
using Reading.Application.Consumers.Media;
using Reading.Application.Consumers.Media.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Media;

public sealed class MediaArticleMediaAttachedReadingHandler
    : MediaReadingIntegrationEventHandler<ArticleMediaAttachedReadingPayload>
{
    public MediaArticleMediaAttachedReadingHandler(
        IMediaReadingEventIngestionService ingestionService,
        ILogger<MediaArticleMediaAttachedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticleMediaAttached;

    protected override string EventDisplayName => "article media attached";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaAttachedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleMediaAttachedAsync(
            context,
            payload,
            cancellationToken);
    }
}
