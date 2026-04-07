using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;
using Interaction.Domain.Enums;

namespace Interaction.Application.UseCases.Comments.UpdateComment;

public sealed class UpdateCommentUseCase : IUpdateCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCommentUseCase(
        ICommentRepository commentRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _commentRepository = commentRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UpdateCommentResponse>> ExecuteAsync(
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
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

        // Persist the updated truth.
        await _commentRepository.UpdateAsync(
            comment,
            cancellationToken);

        return Result<UpdateCommentResponse>.Success(
            new UpdateCommentResponse
            {
                Updated = true
            });
    }
}