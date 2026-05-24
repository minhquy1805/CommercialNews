using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Seo.Payloads;
using Reading.Application.Models.Results;

namespace Reading.Application.Consumers.Seo;

public interface ISeoReadingEventIngestionService
{
    Task<Result<ArticleProjectionApplyResult>> IngestSlugRouteChangedAsync(
        SeoReadingEnvelopeContext context,
        SlugRouteChangedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestSlugRouteDeactivatedAsync(
        SeoReadingEnvelopeContext context,
        SlugRouteDeactivatedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestMetadataUpdatedAsync(
        SeoReadingEnvelopeContext context,
        SeoMetadataUpdatedReadingPayload payload,
        CancellationToken cancellationToken = default);
}
