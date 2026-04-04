using CommercialNews.BuildingBlocks.Contracts.Common;
using Media.Application.Models.QueryModels;
using Media.Domain.Entities;

namespace Media.Application.Ports.Persistence;

public interface IArticleMediaRepository
{
    Task<(long? ArticleMediaId, int AffectedRows)> AttachAsync(
        long articleId,
        long mediaId,
        bool isPrimary,
        long? createdByUserId,
        CancellationToken cancellationToken = default);

    Task<int> DetachAsync(
        long articleId,
        long mediaId,
        long? deletedByUserId,
        CancellationToken cancellationToken = default);

    Task<int> RestoreAsync(
        long articleId,
        long mediaId,
        long? restoredByUserId,
        CancellationToken cancellationToken = default);

    Task<int> SetPrimaryAsync(
        long articleId,
        long mediaId,
        long? updatedByUserId,
        CancellationToken cancellationToken = default);

    Task<int> ReorderByIdsAsync(
        long articleId,
        IReadOnlyList<(long MediaId, int SortOrder)> orders,
        long? updatedByUserId,
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

    Task<IReadOnlyList<ArticleMediaListResultItem>> SelectByMediaIdAsync(
        long mediaId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}