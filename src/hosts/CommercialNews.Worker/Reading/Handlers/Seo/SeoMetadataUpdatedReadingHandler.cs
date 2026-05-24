using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Seo;
using Reading.Application.Consumers.Seo.Payloads;
using Reading.Application.Models.Results;
using Seo.Application.Outbox;

namespace CommercialNews.Worker.Reading.Handlers.Seo;

public sealed class SeoMetadataUpdatedReadingHandler
    : SeoReadingIntegrationEventHandler<SeoMetadataUpdatedReadingPayload>
{
    public SeoMetadataUpdatedReadingHandler(
        ISeoReadingEventIngestionService ingestionService,
        ILogger<SeoMetadataUpdatedReadingHandler> logger)
        : base(ingestionService, logger)
    {
    }

    public override string EventType => SeoIntegrationEventTypes.MetadataUpdated;

    protected override string EventDisplayName => "metadata updated";

    protected override Task<Result<ArticleProjectionApplyResult>> IngestAsync(
        SeoReadingEnvelopeContext context,
        SeoMetadataUpdatedReadingPayload payload,
        CancellationToken cancellationToken)
    {
        return IngestionService.IngestMetadataUpdatedAsync(
            context,
            payload,
            cancellationToken);
    }
}
