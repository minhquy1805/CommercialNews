using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface IArticleRevisionRepository
    {
        Task<ArticleRevision?> InsertAsync(
            ArticleRevision revision,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleRevision>> GetByArticleIdAsync(
            long articleId,
            CancellationToken cancellationToken = default);

        Task<ArticleRevision?> GetByIdAsync(
            long articleId,
            long revisionId,
            CancellationToken cancellationToken = default);
    }
}