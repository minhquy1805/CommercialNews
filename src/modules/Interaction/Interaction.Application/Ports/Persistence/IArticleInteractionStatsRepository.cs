using Interaction.Application.Models.Results;
using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence;

public interface IArticleInteractionStatsRepository
{
    Task<ArticleInteractionStats?> GetByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);

    Task<MaterializeArticleInteractionStatsResult> MaterializeAsync(
        string articlePublicId,
        string publicationMessageIdCandidate,
        CancellationToken cancellationToken = default);
}