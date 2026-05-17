using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Consumers.Content.Payloads;
using Seo.Application.Contracts.EventApply;

namespace Seo.Application.Consumers.Content;

public interface IContentSeoEventApplyService
{
    Task<Result<SeoEventApplyResult>> ApplyArticleCreatedAsync(
        ContentSeoEnvelopeContext context,
        ArticleCreatedSeoPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<SeoEventApplyResult>> ApplyArticleUpdatedAsync(
        ContentSeoEnvelopeContext context,
        ArticleUpdatedSeoPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<SeoEventApplyResult>> ApplyArticlePublishedAsync(
        ContentSeoEnvelopeContext context,
        ArticlePublishedSeoPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<SeoEventApplyResult>> ApplyArticleUnpublishedAsync(
        ContentSeoEnvelopeContext context,
        ArticleUnpublishedSeoPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<SeoEventApplyResult>> ApplyArticleArchivedAsync(
        ContentSeoEnvelopeContext context,
        ArticleArchivedSeoPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<SeoEventApplyResult>> ApplyArticleSoftDeletedAsync(
        ContentSeoEnvelopeContext context,
        ArticleSoftDeletedSeoPayload payload,
        CancellationToken cancellationToken = default);
}