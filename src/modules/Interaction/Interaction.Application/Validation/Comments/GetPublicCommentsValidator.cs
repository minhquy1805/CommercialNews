using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.GetPublicComments;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Comments;

public static class GetPublicCommentsValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumPageSize = 200;

    public static Error? Validate(GetPublicCommentsRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.ArticlePublicId))
        {
            return InteractionErrors.Article.InvalidArticlePublicId;
        }

        if (request.ArticlePublicId.Trim().Length != PublicIdLength)
        {
            return InteractionErrors.Article.InvalidArticlePublicId;
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

        if (!IsValidSortDirection(request.SortDirection))
        {
            return InteractionErrors.Query.InvalidSortDirection;
        }

        return null;
    }

    public static string NormalizeArticlePublicId(string articlePublicId)
    {
        return articlePublicId.Trim();
    }

    public static string NormalizeSortDirection(string sortDirection)
    {
        return sortDirection.Trim().ToUpperInvariant();
    }

    private static bool IsValidSortDirection(string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortDirection))
        {
            return false;
        }

        return string.Equals(
                   sortDirection.Trim(),
                   "ASC",
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   sortDirection.Trim(),
                   "DESC",
                   StringComparison.OrdinalIgnoreCase);
    }
}