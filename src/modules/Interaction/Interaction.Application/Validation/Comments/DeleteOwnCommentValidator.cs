using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.DeleteOwnComment;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Comments;

public static class DeleteOwnCommentValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(DeleteOwnCommentRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.CommentPublicId))
        {
            return InteractionErrors.Comment.InvalidCommentPublicId;
        }

        if (request.CommentPublicId.Trim().Length != PublicIdLength)
        {
            return InteractionErrors.Comment.InvalidCommentPublicId;
        }

        if (request.ExpectedVersion.HasValue &&
            request.ExpectedVersion.Value < 1)
        {
            return InteractionErrors.Comment.InvalidExpectedVersion;
        }

        return null;
    }

    public static string NormalizeCommentPublicId(string commentPublicId)
    {
        return commentPublicId.Trim();
    }
}