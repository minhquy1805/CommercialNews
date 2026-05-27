using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Comments.CreateComment;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Comments;

public static class CreateCommentValidator
{
    private const int PublicIdLength = 26;
    private const int MaximumContentLength = 2000;

    public static Error? Validate(CreateCommentRequestDto? request)
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

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return InteractionErrors.Comment.ContentRequired;
        }

        if (request.Content.Trim().Length > MaximumContentLength)
        {
            return InteractionErrors.Comment.ContentTooLong;
        }

        return null;
    }

    public static string NormalizeArticlePublicId(string articlePublicId)
    {
        return articlePublicId.Trim();
    }

    public static string NormalizeContent(string content)
    {
        return content.Trim();
    }
}