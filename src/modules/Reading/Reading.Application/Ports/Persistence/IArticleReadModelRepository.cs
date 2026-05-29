using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;

namespace Reading.Application.Ports.Persistence;

public interface IArticleReadModelRepository
{
    /*
      =========================================================
      Public read queries
      =========================================================
    */

    Task<ArticleDetailResult?> SelectByPublicIdAsync(
        GetArticleByPublicIdQuery query,
        CancellationToken cancellationToken = default);

    Task<ArticleDetailResult?> SelectBySlugAsync(
        GetArticleBySlugQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<ArticleListItemResult>> SelectSkipAndTakeAsync(
        GetArticlesQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<ArticleListItemResult>> SearchAsync(
        SearchArticlesQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleListItemResult>> SelectRelatedAsync(
        GetRelatedArticlesQuery query,
        CancellationToken cancellationToken = default);

    /*
      =========================================================
      Content projection writes
      =========================================================
    */

    Task<ArticleProjectionApplyResult> UpsertFromContentAsync(
        ApplyContentArticleProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> MarkNotPublicAsync(
        MarkArticleProjectionNotPublicCommand command,
        CancellationToken cancellationToken = default);

    /*
      =========================================================
      SEO projection writes
      =========================================================
    */

    Task<ArticleProjectionApplyResult> ApplySeoRouteAsync(
        ApplyArticleSeoRouteProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> ApplySeoMetadataAsync(
        ApplyArticleSeoMetadataProjectionCommand command,
        CancellationToken cancellationToken = default);

    /*
      =========================================================
      Identity projection writes
      =========================================================
    */

    Task<ArticleProjectionApplyResult> ApplyAuthorProfileAsync(
        ApplyAuthorProfileProjectionCommand command,
        CancellationToken cancellationToken = default);

    /*
      =========================================================
      Interaction projection writes
      =========================================================
    */

    Task<ArticleProjectionApplyResult> ApplyInteractionCountersAsync(
        ApplyArticleInteractionCounterProjectionCommand command,
        CancellationToken cancellationToken = default);

    /*
      =========================================================
      Media projection writes
      =========================================================
    */

    Task<ArticleProjectionApplyResult> UpsertMediaAttachmentAsync(
        UpsertArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> SetPrimaryMediaAsync(
        SetPrimaryArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> ReorderMediaAsync(
        ReorderArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleProjectionApplyResult> DetachMediaAsync(
        DetachArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);
}