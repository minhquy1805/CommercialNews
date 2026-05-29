using Interaction.Application.Models.Results;
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

    Task<IReadOnlyList<PendingViewStatsMaterializationItemResult>>
        GetPendingStatsMaterializationBatchAsync(
            int batchSize,
            CancellationToken cancellationToken = default);
}