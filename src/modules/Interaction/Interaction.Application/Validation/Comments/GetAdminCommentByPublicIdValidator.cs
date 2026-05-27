using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Comments;

public static class GetAdminCommentByPublicIdValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(
        GetAdminCommentByPublicIdRequestDto? request)
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

        return null;
    }

    public static string NormalizeCommentPublicId(string commentPublicId)
    {
        return commentPublicId.Trim();
    }
}