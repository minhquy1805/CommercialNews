using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;

namespace Interaction.Application.UseCases.LikeArticle;

public sealed class LikeArticleUseCase : ILikeArticleUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LikeArticleUseCase(
        IArticleLikeRepository articleLikeRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _articleLikeRepository = articleLikeRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LikeArticleResponse>> ExecuteAsync(
        LikeArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate basic input first.
        if (request.ArticleId <= 0)
        {
            return Result<LikeArticleResponse>.Failure(
                InteractionErrors.Article.InvalidArticleId);
        }

        if (request.UserId <= 0)
        {
            return Result<LikeArticleResponse>.Failure(
                InteractionErrors.Like.InvalidUserId);
        }

        // Read current like truth for this article/user pair.
        ArticleLike? currentLike = await _articleLikeRepository.GetByArticleIdAndUserIdAsync(
            request.ArticleId,
            request.UserId,
            cancellationToken);

        // Create the first active like if no row exists yet.
        if (currentLike is null)
        {
            var articleLike = ArticleLike.Create(
                articleId: request.ArticleId,
                userId: request.UserId,
                nowUtc: _dateTimeProvider.UtcNow);

            await _articleLikeRepository.InsertAsync(
                articleLike,
                cancellationToken);

            return Result<LikeArticleResponse>.Success(
                new LikeArticleResponse
                {
                    Liked = true
                });
        }

        // If already liked, keep the result deterministic.
        if (currentLike.IsActive)
        {
            return Result<LikeArticleResponse>.Success(
                new LikeArticleResponse
                {
                    Liked = true
                });
        }

        // Reactivate the existing row if it is currently inactive.
        await _articleLikeRepository.ActivateAsync(
            request.ArticleId,
            request.UserId,
            cancellationToken);

        return Result<LikeArticleResponse>.Success(
            new LikeArticleResponse
            {
                Liked = true
            });
    }
}