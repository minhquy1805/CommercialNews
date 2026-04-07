using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence.Write;

public interface IArticleInteractionStatsRepository
{
    Task<ArticleInteractionStats?> GetByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        ArticleInteractionStats stats,
        CancellationToken cancellationToken = default);
}