using Media.Domain.Entities;

namespace Media.Application.Ports.Persistence;

public interface IArticleMediaSetRepository
{
    Task<ArticleMediaSet?> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);
}