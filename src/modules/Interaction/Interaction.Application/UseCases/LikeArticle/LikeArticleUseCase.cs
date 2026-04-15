using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Exceptions;

namespace Interaction.Application.UseCases.LikeArticle;

public sealed class LikeArticleUseCase : ILikeArticleUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LikeArticleUseCase(
        IArticleLikeRepository articleLikeRepository,
        IInteractionUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _articleLikeRepository = articleLikeRepository
            ?? throw new ArgumentNullException(nameof(articleLikeRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<LikeArticleResponse>> ExecuteAsync(
        LikeArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
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

            // Keep the command deterministic if the article is already liked.
            if (currentLike is not null && currentLike.IsActive)
            {
                return Result<LikeArticleResponse>.Success(
                    new LikeArticleResponse
                    {
                        Liked = true
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Create the first active like if no row exists yet.
                if (currentLike is null)
                {
                    ArticleLike articleLike = ArticleLike.Create(
                        articleId: request.ArticleId,
                        userId: request.UserId,
                        nowUtc: _dateTimeProvider.UtcNow);

                    await _articleLikeRepository.InsertAsync(
                        articleLike,
                        cancellationToken);
                }
                else
                {
                    // Reactivate the existing row if it is currently inactive.
                    currentLike.Activate(_dateTimeProvider.UtcNow);

                    await _articleLikeRepository.ActivateAsync(
                        request.ArticleId,
                        request.UserId,
                        cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<LikeArticleResponse>.Success(
                    new LikeArticleResponse
                    {
                        Liked = true
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<LikeArticleResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (InteractionDomainException exception)
        {
            return Result<LikeArticleResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(InteractionDomainException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.ARTICLE_LIKE_INVALID_ARTICLE_ID" => InteractionErrors.Article.InvalidArticleId,
            "INTERACTION.ARTICLE_LIKE_INVALID_USER_ID" => InteractionErrors.Like.InvalidUserId,
            "INTERACTION.ARTICLE_LIKE_INVALID_ACTIVE_STATE" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_LIKE_INVALID_INACTIVE_STATE" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_LIKE_INVALID_CREATED_AT" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_LIKE_INVALID_LIKED_AT" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_LIKE_INVALID_LIKED_AT_ORDER" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_LIKE_INVALID_UPDATED_AT_ORDER" => InteractionErrors.ValidationFailed,
            "INTERACTION.ARTICLE_LIKE_INVALID_UNLIKED_AT_ORDER" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.ARTICLE_NOT_FOUND" => InteractionErrors.Article.NotFound,
            "INTERACTION.LIKE_ALREADY_EXISTS" => Error.Conflict(
                code: "INTERACTION.LIKE_ALREADY_EXISTS",
                message: "A like already exists for this article and user."),
            "INTERACTION.LIKE_INVALID_STATE" => InteractionErrors.ValidationFailed,
            "INTERACTION.VALIDATION_FAILED" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }
}