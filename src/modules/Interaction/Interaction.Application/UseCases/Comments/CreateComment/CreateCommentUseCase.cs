using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Comments.Requests;
using Interaction.Application.Contracts.Comments.Responses;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Domain.Entities;

namespace Interaction.Application.UseCases.Comments.CreateComment;

public sealed class CreateCommentUseCase : ICreateCommentUseCase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCommentUseCase(
        ICommentRepository commentRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _commentRepository = commentRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateCommentResponse>> ExecuteAsync(
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
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
        var comment = Comment.Create(
            articleId: request.ArticleId,
            userId: request.UserId,
            parentCommentId: request.ParentCommentId,
            content: content,
            nowUtc: _dateTimeProvider.UtcNow);

        // Persist the new comment row.
        long commentId = await _commentRepository.InsertAsync(
            comment,
            cancellationToken);

        var response = new CreateCommentResponse
        {
            CommentId = commentId,
            CreatedAt = comment.CreatedAt
        };

        return Result<CreateCommentResponse>.Success(response);
    }
}