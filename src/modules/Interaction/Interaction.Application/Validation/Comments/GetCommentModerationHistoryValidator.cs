using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetCommentModerationHistory;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Comments;

public static class GetCommentModerationHistoryValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumPageSize = 200;

    public static Error? Validate(
        GetCommentModerationHistoryRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.CommentPublicId) ||
            request.CommentPublicId.Trim().Length != PublicIdLength)
        {
            return InteractionErrors.Comment.InvalidCommentPublicId;
        }

        if (request.Page < 1)
        {
            return InteractionErrors.Query.InvalidPage;
        }

        if (request.PageSize < 1)
        {
            return InteractionErrors.Query.InvalidPageSize;
        }

        if (request.PageSize > MaximumPageSize)
        {
            return InteractionErrors.Query.PageSizeTooLarge;
        }

        return null;
    }

    public static string NormalizeCommentPublicId(string commentPublicId)
    {
        return commentPublicId.Trim();
    }
}