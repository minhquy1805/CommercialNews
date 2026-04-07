using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence.Write;

public interface IArticleLikeRepository
{
    Task<long> InsertAsync(
        ArticleLike articleLike,
        CancellationToken cancellationToken = default);

    Task<ArticleLike?> GetByArticleIdAndUserIdAsync(
        long articleId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<int> ActivateAsync(
        long articleId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<int> DeactivateAsync(
        long articleId,
        long userId,
        CancellationToken cancellationToken = default);
}