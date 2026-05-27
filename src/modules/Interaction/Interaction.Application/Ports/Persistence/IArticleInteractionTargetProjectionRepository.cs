using Interaction.Application.Models.Results;
using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence;

public interface IArticleInteractionTargetProjectionRepository
{
    Task<ArticleInteractionTargetProjection?> GetByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);

    Task<ApplyArticleInteractionTargetProjectionResult> ApplyAsync(
        string articlePublicId,
        string sourceStatus,
        bool isInteractionEnabled,
        long sourceVersion,
        string sourceMessageId,
        DateTime? sourceOccurredAtUtc,
        CancellationToken cancellationToken = default);
}