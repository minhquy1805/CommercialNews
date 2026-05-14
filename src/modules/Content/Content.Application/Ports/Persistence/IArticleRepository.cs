using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Content.Application.Models.QueryModels;
using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface IArticleRepository
    {
        Task<(long ArticleId, long Version)> InsertAsync(
            Article article,
            CancellationToken cancellationToken = default);

        Task<Article?> GetByIdAsync(
            long articleId,
            CancellationToken cancellationToken = default);

        Task<Article?> GetByPublicIdAsync(
            string articlePublicId,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<ArticleListResultItem>> GetPagedAsync(
            ArticleListQuery query,
            CancellationToken cancellationToken = default);

        Task<Article?> UpdateAsync(
            Article article,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> PublishAsync(
            long articleId,
            long? actorUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> UnpublishAsync(
            long articleId,
            long? actorUserId,
            long expectedVersion,
            string reason,
            CancellationToken cancellationToken = default);

        Task<Article?> ArchiveAsync(
            long articleId,
            long? actorUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> SoftDeleteAsync(
            long articleId,
            long? actorUserId,
            long expectedVersion,
            CancellationToken cancellationToken = default);
    }
}