using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;

namespace Interaction.Application.UseCases.UnlikeArticle;

public sealed class UnlikeArticleUseCase : IUnlikeArticleUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;

    public UnlikeArticleUseCase(IArticleLikeRepository articleLikeRepository)
    {
        _articleLikeRepository = articleLikeRepository;
    }

    public async Task<Result<UnlikeArticleResponse>> ExecuteAsync(
        UnlikeArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate basic input first.
        if (request.ArticleId <= 0)
        {
            return Result<UnlikeArticleResponse>.Failure(
                InteractionErrors.Article.InvalidArticleId);
        }

        if (request.UserId <= 0)
        {
            return Result<UnlikeArticleResponse>.Failure(
                InteractionErrors.Like.InvalidUserId);
        }

        // Read current like truth for this article/user pair.
        ArticleLike? currentLike = await _articleLikeRepository.GetByArticleIdAndUserIdAsync(
            request.ArticleId,
            request.UserId,
            cancellationToken);

        // If no row exists, keep the result deterministic.
        if (currentLike is null)
        {
            return Result<UnlikeArticleResponse>.Success(
                new UnlikeArticleResponse
                {
                    Liked = false
                });
        }

        // If already inactive, keep the result deterministic.
        if (!currentLike.IsActive)
        {
            return Result<UnlikeArticleResponse>.Success(
                new UnlikeArticleResponse
                {
                    Liked = false
                });
        }

        // Deactivate the current active like row.
        await _articleLikeRepository.DeactivateAsync(
            request.ArticleId,
            request.UserId,
            cancellationToken);

        return Result<UnlikeArticleResponse>.Success(
            new UnlikeArticleResponse
            {
                Liked = false
            });
    }
}