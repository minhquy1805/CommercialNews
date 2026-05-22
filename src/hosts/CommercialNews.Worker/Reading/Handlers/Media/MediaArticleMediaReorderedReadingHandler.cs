using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;
using Reading.Application.Consumers.Media;
using Reading.Application.Consumers.Media.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Media;

public sealed class MediaArticleMediaReorderedReadingHandler
    : MediaReadingIntegrationEventHandler<ArticleMediaReorderedReadingPayload>
{
    public MediaArticleMediaReorderedReadingHandler(
        IMediaReadingEventIngestionService ingestionService,
        ILogger<MediaArticleMediaReorderedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticleMediaReordered;

    protected override string EventDisplayName => "article media reordered";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaReorderedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticleMediaReorderedAsync(
            context,
            payload,
            cancellationToken);
    }
}
