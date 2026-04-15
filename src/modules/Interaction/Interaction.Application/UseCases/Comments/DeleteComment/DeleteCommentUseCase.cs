using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Enums;
using Interaction.Domain.Exceptions;

namespace Interaction.Application.UseCases.Comments.DeleteComment;

public sealed class DeleteCommentUseCase : IDeleteCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCommentUseCase(
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

    public async Task<Result<DeleteCommentResponse>> ExecuteAsync(
        DeleteCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the main input first.
            if (request.CommentId <= 0)
            {
                return Result<DeleteCommentResponse>.Failure(
                    InteractionErrors.Comment.InvalidCommentId);
            }

            if (request.UserId <= 0)
            {
                return Result<DeleteCommentResponse>.Failure(
                    InteractionErrors.ValidationFailed);
            }

            // Load current comment truth.
            Comment? comment = await _commentRepository.GetByIdAsync(
                request.CommentId,
                cancellationToken);

            if (comment is null)
            {
                return Result<DeleteCommentResponse>.Failure(
                    InteractionErrors.Comment.NotFound);
            }

            // Only the owner can delete the comment in V1.
            if (comment.UserId != request.UserId)
            {
                return Result<DeleteCommentResponse>.Failure(
                    InteractionErrors.Comment.NotOwner);
            }

            // Keep the command idempotent.
            if (string.Equals(comment.Status, CommentStatus.Deleted, StringComparison.OrdinalIgnoreCase))
            {
                return Result<DeleteCommentResponse>.Success(
                    new DeleteCommentResponse
                    {
                        Deleted = true
                    });
            }

            // Apply the delete in the domain layer first.
            comment.SoftDelete(
                _dateTimeProvider.UtcNow,
                request.UserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Persist the deleted truth inside the transaction.
                await _commentRepository.SoftDeleteAsync(
                    commentId: request.CommentId,
                    deletedByUserId: request.UserId,
                    expectedUserId: request.UserId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<DeleteCommentResponse>.Success(
                    new DeleteCommentResponse
                    {
                        Deleted = true
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
            return Result<DeleteCommentResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (InteractionDomainException exception)
        {
            return Result<DeleteCommentResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(InteractionDomainException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.COMMENT_INVALID_ID" => InteractionErrors.Comment.InvalidCommentId,
            "INTERACTION.COMMENT_ALREADY_DELETED" => InteractionErrors.Comment.AlreadyDeleted,
            "INTERACTION.COMMENT_INVALID_DELETED_BY_USER_ID" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.COMMENT_NOT_FOUND" => InteractionErrors.Comment.NotFound,
            "INTERACTION.VALIDATION_FAILED" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }
}