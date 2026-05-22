using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Outbox;
using Reading.Application.Consumers.Media;
using Reading.Application.Consumers.Media.Payloads;
using Reading.Application.Models.Results;

namespace CommercialNews.Worker.Reading.Handlers.Media;

public sealed class MediaArticlePrimaryMediaSetReadingHandler
    : MediaReadingIntegrationEventHandler<ArticlePrimaryMediaSetReadingPayload>
{
    public MediaArticlePrimaryMediaSetReadingHandler(
        IMediaReadingEventIngestionService ingestionService,
        ILogger<MediaArticlePrimaryMediaSetReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => MediaIntegrationEventTypes.ArticlePrimaryMediaSet;

    protected override string EventDisplayName => "article primary media set";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        MediaReadingEnvelopeContext context,
        ArticlePrimaryMediaSetReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestArticlePrimaryMediaSetAsync(
            context,
            payload,
            cancellationToken);
    }
}
