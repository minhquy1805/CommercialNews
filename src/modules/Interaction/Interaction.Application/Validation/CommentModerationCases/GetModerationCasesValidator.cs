using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.CommentModerationCases.GetModerationCases;
using Interaction.Application.Errors;
using Interaction.Domain.Constants;

namespace Interaction.Application.Validation.CommentModerationCases;

public static class GetModerationCasesValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumPageSize = 200;

    public static Error? Validate(GetModerationCasesRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (request.Status is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Status) ||
                !CommentModerationCaseStatuses.IsValid(request.Status.Trim()))
            {
                return InteractionErrors.Query.InvalidCaseStatus;
            }
        }

        if (request.Priority is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Priority) ||
                !CommentModerationCasePriorities.IsValid(request.Priority.Trim()))
            {
                return InteractionErrors.Query.InvalidCasePriority;
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

        if (request.CommentPublicId is not null)
        {
            if (string.IsNullOrWhiteSpace(request.CommentPublicId) ||
                request.CommentPublicId.Trim().Length != PublicIdLength)
            {
                return InteractionErrors.Comment.InvalidCommentPublicId;
            }
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

    public static string? NormalizePriority(string? priority)
    {
        return string.IsNullOrWhiteSpace(priority)
            ? null
            : priority.Trim();
    }

    public static string? NormalizeArticlePublicId(string? articlePublicId)
    {
        return string.IsNullOrWhiteSpace(articlePublicId)
            ? null
            : articlePublicId.Trim();
    }

    public static string? NormalizeCommentPublicId(string? commentPublicId)
    {
        return string.IsNullOrWhiteSpace(commentPublicId)
            ? null
            : commentPublicId.Trim();
    }
}