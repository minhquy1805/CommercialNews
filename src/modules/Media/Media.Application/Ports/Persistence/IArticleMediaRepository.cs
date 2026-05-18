using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Media.Application.Models.Commands;
using Media.Application.Models.Queries;
using Media.Application.Models.Results;
using Media.Domain.Entities;

namespace Media.Application.Ports.Persistence;

public interface IArticleMediaRepository
{
    Task<ArticleMediaAttachResult> AttachAsync(
        AttachArticleMediaCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleMediaDetachResult> DetachAsync(
        DetachArticleMediaCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleMediaMutationResult> RestoreAsync(
        RestoreArticleMediaCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleMediaMutationResult> SetPrimaryAsync(
        SetPrimaryArticleMediaCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleMediaMutationResult> ReorderByIdsAsync(
        ReorderArticleMediaCommand command,
        CancellationToken cancellationToken = default);

    Task<ArticleMedia?> GetByIdAsync(
        long articleMediaId,
        CancellationToken cancellationToken = default);

    Task<ArticleMediaListResultItem?> GetPrimaryByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<ArticleMediaListResultItem>> SelectByArticleIdAsync(
        ArticleMediaListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleMediaUsageResultItem>> SelectByMediaIdAsync(
        long mediaId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}