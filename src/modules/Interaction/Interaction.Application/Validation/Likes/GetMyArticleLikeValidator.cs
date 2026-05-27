using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.Likes.GetMyArticleLike;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.Likes;

public static class GetMyArticleLikeValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(GetMyArticleLikeRequestDto? request)
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

        return null;
    }

    public static string NormalizeArticlePublicId(string articlePublicId)
    {
        return articlePublicId.Trim();
    }
}