using Interaction.Application.Models.Results;
using Interaction.Domain.Entities;

namespace Interaction.Application.Ports.Persistence;

public interface IArticleLikeRepository
{
    Task<ArticleLike?> GetByArticlePublicIdAndUserIdAsync(
        string articlePublicId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<ArticleLikeMutationResult> SetLikedAsync(
        string publicId,
        string articlePublicId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<ArticleLikeMutationResult> SetUnlikedAsync(
        string articlePublicId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<long> GetActiveCountByArticlePublicIdAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);
}