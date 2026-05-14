using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence
{
    public interface IArticleLifecycleEventRepository
    {
        Task<ArticleLifecycleEvent?> InsertAsync(
            ArticleLifecycleEvent lifecycleEvent,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleLifecycleEvent>> GetByArticleIdAsync(
            long articleId,
            CancellationToken cancellationToken = default);
    }
}