using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Exceptions;

namespace Interaction.Application.UseCases.UnlikeArticle;

public sealed class UnlikeArticleUseCase : IUnlikeArticleUseCase
{
    private readonly IArticleLikeRepository _articleLikeRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnlikeArticleUseCase(
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

    public async Task<Result<UnlikeArticleResponse>> ExecuteAsync(
        UnlikeArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
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

            // Keep the command deterministic if no row exists.
            if (currentLike is null)
            {
                return Result<UnlikeArticleResponse>.Success(
                    new UnlikeArticleResponse
                    {
                        Liked = false
                    });
            }

            // Keep the command deterministic if already inactive.
            if (!currentLike.IsActive)
            {
                return Result<UnlikeArticleResponse>.Success(
                    new UnlikeArticleResponse
                    {
                        Liked = false
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Apply the unlike change inside the transaction.
                currentLike.Deactivate(_dateTimeProvider.UtcNow);

                await _articleLikeRepository.DeactivateAsync(
                    request.ArticleId,
                    request.UserId,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UnlikeArticleResponse>.Success(
                    new UnlikeArticleResponse
                    {
                        Liked = false
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
            return Result<UnlikeArticleResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (InteractionDomainException exception)
        {
            return Result<UnlikeArticleResponse>.Failure(
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
            "INTERACTION.LIKE_INVALID_STATE" => InteractionErrors.ValidationFailed,
            "INTERACTION.VALIDATION_FAILED" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }
}