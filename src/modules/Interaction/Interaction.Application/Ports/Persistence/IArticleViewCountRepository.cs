using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence;

public interface IArticleViewCountRepository
{
    Task<ArticleViewCount?> GetByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);

    Task<ArticleViewCount> IncrementAcceptedAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);
}