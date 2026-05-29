using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.ArticleInteractionStats;

public static class GetArticleInteractionStatsValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(
        GetArticleInteractionStatsRequestDto? request)
    {
        if (request is null)
        {
            return InteractionErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.ArticlePublicId) ||
            request.ArticlePublicId.Trim().Length != PublicIdLength)
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