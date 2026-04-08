using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Enums;
using Interaction.Domain.Exceptions;

namespace Interaction.Application.UseCases.Comments.UpdateComment;

public sealed class UpdateCommentUseCase : IUpdateCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCommentUseCase(
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

    public async Task<Result<UpdateCommentResponse>> ExecuteAsync(
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the main input first.
            if (request.CommentId <= 0)
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.Comment.InvalidCommentId);
            }

            if (request.UserId <= 0)
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.Comment.ContentRequired);
            }

            string content = request.Content.Trim();

            if (content.Length > 2000)
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.Comment.ContentTooLong);
            }

            // Load current comment truth.
            Comment? comment = await _commentRepository.GetByIdAsync(
                request.CommentId,
                cancellationToken);

            if (comment is null)
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.Comment.NotFound);
            }

            // Only the owner can update the comment in V1.
            if (comment.UserId != request.UserId)
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.Comment.NotOwner);
            }

            // Deleted comments cannot be changed.
            if (string.Equals(comment.Status, CommentStatus.Deleted, StringComparison.OrdinalIgnoreCase))
            {
                return Result<UpdateCommentResponse>.Failure(
                    InteractionErrors.Comment.AlreadyDeleted);
            }

            // Apply the change in the domain layer first.
            comment.Update(
                content,
                _dateTimeProvider.UtcNow);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Persist the updated truth inside the transaction.
                await _commentRepository.UpdateAsync(
                    comment,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdateCommentResponse>.Success(
                    new UpdateCommentResponse
                    {
                        Updated = true
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
            return Result<UpdateCommentResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (InteractionDomainException exception)
        {
            return Result<UpdateCommentResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(InteractionDomainException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.COMMENT_INVALID_ID" => InteractionErrors.Comment.InvalidCommentId,
            "INTERACTION.COMMENT_CONTENT_REQUIRED" => InteractionErrors.Comment.ContentRequired,
            "INTERACTION.COMMENT_CONTENT_TOO_LONG" => InteractionErrors.Comment.ContentTooLong,
            "INTERACTION.COMMENT_ALREADY_DELETED" => InteractionErrors.Comment.AlreadyDeleted,
            _ => InteractionErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "INTERACTION.COMMENT_NOT_FOUND" => InteractionErrors.Comment.NotFound,
            "INTERACTION.COMMENT_CONTENT_REQUIRED" => InteractionErrors.Comment.ContentRequired,
            "INTERACTION.COMMENT_INVALID_STATUS" => InteractionErrors.Comment.InvalidStatus,
            "INTERACTION.VALIDATION_FAILED" => InteractionErrors.ValidationFailed,
            _ => InteractionErrors.ValidationFailed
        };
    }
}