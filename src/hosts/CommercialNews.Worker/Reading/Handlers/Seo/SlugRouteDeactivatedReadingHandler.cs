using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Seo;
using Reading.Application.Consumers.Seo.Payloads;
using Reading.Application.Models.Results;
using Seo.Application.Outbox;

namespace CommercialNews.Worker.Reading.Handlers.Seo;

public sealed class SlugRouteDeactivatedReadingHandler
    : SeoReadingIntegrationEventHandler<SlugRouteDeactivatedReadingPayload>
{
    public SlugRouteDeactivatedReadingHandler(
        ISeoReadingEventIngestionService ingestionService,
        ILogger<SlugRouteDeactivatedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => SeoIntegrationEventTypes.SlugRouteDeactivated;

    protected override string EventDisplayName => "slug route deactivated";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        SeoReadingEnvelopeContext context,
        SlugRouteDeactivatedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestSlugRouteDeactivatedAsync(
            context,
            payload,
            cancellationToken);
    }
}
