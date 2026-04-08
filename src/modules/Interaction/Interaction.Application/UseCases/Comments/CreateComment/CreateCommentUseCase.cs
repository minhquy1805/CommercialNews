using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Exceptions;

namespace Interaction.Application.UseCases.Comments.CreateComment;

public sealed class CreateCommentUseCase : ICreateCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCommentUseCase(
        ICommentRepository commentRepository,
        IInteractionUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _commentRepository = commentRepository
            ?? throw new ArgumentNullException(nameof(commentRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<CreateCommentResponse>> ExecuteAsync(
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the main input first.
            if (request.ArticleId <= 0)
            {
                return Result<CreateCommentResponse>.Failure(
                    InteractionErrors.Article.InvalidArticleId);
            }

            if (request.UserId <= 0)
            {
                return Result<CreateCommentResponse>.Failure(
                    InteractionErrors.ValidationFailed);
            }

            if (request.ParentCommentId.HasValue && request.ParentCommentId.Value <= 0)
            {
                return Result<CreateCommentResponse>.Failure(
                    InteractionErrors.Comment.InvalidParentCommentId);
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Result<CreateCommentResponse>.Failure(
                    InteractionErrors.Comment.ContentRequired);
            }

            string content = request.Content.Trim();

            if (content.Length > 2000)
            {
                return Result<CreateCommentResponse>.Failure(
                    InteractionErrors.Comment.ContentTooLong);
            }

            // Create comment truth in the domain layer.
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            Comment comment = Comment.Create(
                articleId: request.ArticleId,
                userId: request.UserId,
                parentCommentId: request.ParentCommentId,
                content: content,
                nowUtc: nowUtc);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Persist the new comment row inside the transaction.
                long commentId = await _commentRepository.InsertAsync(
                    comment,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreateCommentResponse>.Success(
                    new CreateCommentResponse
                    {
                        CommentId = commentId,
                        CreatedAt = comment.CreatedAt
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
            return Result<CreateCommentResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (InteractionDomainException exception)
        {
            return Result<CreateCommentResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(InteractionDomainException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.COMMENT_INVALID_ARTICLE_ID" => InteractionErrors.Article.InvalidArticleId,
            "INTERACTION.COMMENT_INVALID_PARENT_COMMENT_ID" => InteractionErrors.Comment.InvalidParentCommentId,
            "INTERACTION.COMMENT_CONTENT_REQUIRED" => InteractionErrors.Comment.ContentRequired,
            "INTERACTION.COMMENT_CONTENT_TOO_LONG" => InteractionErrors.Comment.ContentTooLong,
            "INTERACTION.COMMENT_INVALID_STATUS" => InteractionErrors.Comment.InvalidStatus,
            _ => InteractionErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.ARTICLE_NOT_FOUND" => InteractionErrors.Article.NotFound,
            "INTERACTION.PARENT_COMMENT_NOT_FOUND" => InteractionErrors.Comment.InvalidParentCommentId,
            "INTERACTION.COMMENT_CONTENT_REQUIRED" => InteractionErrors.Comment.ContentRequired,
            "INTERACTION.COMMENT_INVALID_STATUS" => InteractionErrors.Comment.InvalidStatus,
            _ => InteractionErrors.ValidationFailed
        };
    }
}