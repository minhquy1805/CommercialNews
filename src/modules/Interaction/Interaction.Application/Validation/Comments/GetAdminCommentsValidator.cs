using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetAdminComments;
using Interaction.Application.Errors;
using Interaction.Domain.Constants;

namespace Interaction.Application.Validation.Comments;

public static class GetAdminCommentsValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumPageSize = 200;

    public static Error? Validate(GetAdminCommentsRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (request.Status is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Status) ||
                !CommentStatuses.IsValid(request.Status.Trim()))
            {
                return InteractionErrors.Query.InvalidCommentStatus;
            }
        }

        if (request.ArticlePublicId is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ArticlePublicId) ||
                request.ArticlePublicId.Trim().Length != PublicIdLength)
            {
                return InteractionErrors.Article.InvalidArticlePublicId;
            }
        }

        if (request.AuthorUserId.HasValue &&
            request.AuthorUserId.Value <= 0)
        {
            return InteractionErrors.Query.InvalidAuthorUserId;
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

    public static string? NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim();
    }

    public static string? NormalizeArticlePublicId(string? articlePublicId)
    {
        return string.IsNullOrWhiteSpace(articlePublicId)
            ? null
            : articlePublicId.Trim();
    }
}