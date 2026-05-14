using Content.Domain.Entities;

namespace Content.Application.Ports.Persistence;

public interface IArticleTagRepository
{
    Task<ArticleTag?> InsertAsync(
        ArticleTag articleTag,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteByArticleIdAndTagIdAsync(
        long articleId,
        long tagId,
        CancellationToken cancellationToken = default);

    Task<int> DeleteAllByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArticleTag>> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);
}