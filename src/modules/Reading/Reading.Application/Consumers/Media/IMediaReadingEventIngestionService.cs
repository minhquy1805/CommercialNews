using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Media.Payloads;
using Reading.Application.Models.Results;

namespace Reading.Application.Consumers.Media;

public interface IMediaReadingEventIngestionService
{
    Task<Result<ArticleProjectionApplyResult>> IngestArticleMediaAttachedAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaAttachedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticlePrimaryMediaSetAsync(
        MediaReadingEnvelopeContext context,
        ArticlePrimaryMediaSetReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticleMediaReorderedAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaReorderedReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestArticleMediaDetachedAsync(
        MediaReadingEnvelopeContext context,
        ArticleMediaDetachedReadingPayload payload,
        CancellationToken cancellationToken = default);
}
