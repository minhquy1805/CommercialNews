using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Enums;

namespace Interaction.Application.UseCases.Comments.DeleteComment;

public sealed class DeleteCommentUseCase : IDeleteCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCommentUseCase(
        ICommentRepository commentRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _commentRepository = commentRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<DeleteCommentResponse>> ExecuteAsync(
        DeleteCommentRequest request,
        CancellationToken cancellationToken = default)
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

        // Apply soft delete in the domain layer first.
        comment.SoftDelete(
            _dateTimeProvider.UtcNow,
            request.UserId);

        // Persist the deleted truth.
        await _commentRepository.SoftDeleteAsync(
            commentId: request.CommentId,
            deletedByUserId: request.UserId,
            expectedUserId: request.UserId,
            cancellationToken: cancellationToken);

        return Result<DeleteCommentResponse>.Success(
            new DeleteCommentResponse
            {
                Deleted = true
            });
    }
}