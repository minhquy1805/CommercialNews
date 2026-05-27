using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.ArticleInteractionStats;

public static class MaterializeArticleInteractionStatsValidator
{
    private const int PublicIdLength = 26;

    public static Error? Validate(
        MaterializeArticleInteractionStatsRequestDto? request)
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