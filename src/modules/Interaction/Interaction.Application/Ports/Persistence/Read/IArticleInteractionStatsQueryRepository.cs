using Interaction.Application.Models.QueryModels;

namespace Interaction.Application.Ports.Persistence.Read;

public interface IArticleInteractionStatsQueryRepository
{
    Task<ArticleCountersResult?> GetCountersByArticleIdAsync(
        long articleId,
        CancellationToken cancellationToken = default);
}