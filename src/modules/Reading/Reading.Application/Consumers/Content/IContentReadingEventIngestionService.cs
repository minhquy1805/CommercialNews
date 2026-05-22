using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Content.Payloads;
using Reading.Application.Models.Results;

namespace Reading.Application.Consumers.Content;

public interface IContentReadingEventIngestionService
{
    Task<Result<ArticleProjectionApplyResult>> IngestArticlePublishedAsync(
        ContentReadingEnvelopeContext context,
        ArticlePublishedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticleUpdatedAsync(
        ContentReadingEnvelopeContext context,
        ArticleUpdatedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticleUnpublishedAsync(
        ContentReadingEnvelopeContext context,
        ArticleUnpublishedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticleArchivedAsync(
        ContentReadingEnvelopeContext context,
        ArticleArchivedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticleSoftDeletedAsync(
        ContentReadingEnvelopeContext context,
        ArticleSoftDeletedReadingPayload payload,
        CancellationToken cancellationToken = default);
}