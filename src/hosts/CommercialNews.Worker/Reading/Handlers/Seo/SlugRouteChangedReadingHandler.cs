using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Seo;
using Reading.Application.Consumers.Seo.Payloads;
using Reading.Application.Models.Results;
using Seo.Application.Outbox;

namespace CommercialNews.Worker.Reading.Handlers.Seo;

public sealed class SlugRouteChangedReadingHandler
    : SeoReadingIntegrationEventHandler<SlugRouteChangedReadingPayload>
{
    public SlugRouteChangedReadingHandler(
        ISeoReadingEventIngestionService ingestionService,
        ILogger<SlugRouteChangedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => SeoIntegrationEventTypes.SlugRouteChanged;

    protected override string EventDisplayName => "slug route changed";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        SeoReadingEnvelopeContext context,
        SlugRouteChangedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestSlugRouteChangedAsync(
            context,
            payload,
            cancellationToken);
    }
}
