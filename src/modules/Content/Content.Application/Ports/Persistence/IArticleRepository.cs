using Content.Application.Models.QueryModels;
using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface IArticleRepository
    {
        Task<(long ArticleId, int Version)> InsertAsync(
            Article article,
            CancellationToken cancellationToken = default);

        Task<Article?> GetByIdAsync(
            long articleId,
            CancellationToken cancellationToken = default);

        Task<Article?> GetByPublicIdAsync(
            string publicId,
            CancellationToken cancellationToken = default);

        Task<PagedQueryResult<ArticleListResultItem>> GetPagedAsync(
            ArticleListQuery query,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            Article article,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> PublishAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> UnpublishAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> ArchiveAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> RestoreAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);

        Task<Article?> DeleteAsync(
            long articleId,
            long? actorUserId,
            int expectedVersion,
            CancellationToken cancellationToken = default);
    }
}