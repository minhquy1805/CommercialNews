using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId;
using Interaction.Application.Errors;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Validation.Comments;
using Interaction.Domain.Entities;

namespace Interaction.Application.UseCases.Comments.GetAdminCommentByPublicId;

public sealed class GetAdminCommentByPublicIdUseCase
    : IGetAdminCommentByPublicIdUseCase
{
    private readonly ICommentRepository _commentRepository;

    public GetAdminCommentByPublicIdUseCase(
        ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository
            ?? throw new ArgumentNullException(nameof(commentRepository));
    }

    public async Task<Result<GetAdminCommentByPublicIdResponseDto>> ExecuteAsync(
        GetAdminCommentByPublicIdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            GetAdminCommentByPublicIdValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<GetAdminCommentByPublicIdResponseDto>.Failure(
                validationError);
        }

        var commentPublicId =
            GetAdminCommentByPublicIdValidator.NormalizeCommentPublicId(
                request.CommentPublicId);

        var comment = await _commentRepository.GetByPublicIdAsync(
            commentPublicId,
            cancellationToken);

        if (comment is null)
        {
            return Result<GetAdminCommentByPublicIdResponseDto>.Failure(
                InteractionErrors.Comment.NotFound);
        }

        return Result<GetAdminCommentByPublicIdResponseDto>.Success(
            MapResponse(comment));
    }

    private static GetAdminCommentByPublicIdResponseDto MapResponse(
        Comment comment)
    {
        return new GetAdminCommentByPublicIdResponseDto
        {
            CommentPublicId = comment.PublicId,
            ArticlePublicId = comment.ArticlePublicId,
            AuthorUserId = comment.AuthorUserId,
            Content = comment.Content,
            Status = comment.Status,
            ParentCommentId = comment.ParentCommentId,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpdatedAtUtc = comment.UpdatedAtUtc,
            DeletedAtUtc = comment.DeletedAtUtc,
            Version = comment.Version
        };
    }
}