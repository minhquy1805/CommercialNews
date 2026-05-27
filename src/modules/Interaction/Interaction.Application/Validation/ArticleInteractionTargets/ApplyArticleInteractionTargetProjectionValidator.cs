using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;
using Interaction.Application.Errors;

namespace Interaction.Application.Validation.ArticleInteractionTargets;

public static class ApplyArticleInteractionTargetProjectionValidator
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    public static Error? Validate(
        ApplyArticleInteractionTargetProjectionRequestDto? request)
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

        if (string.IsNullOrWhiteSpace(request.SourceStatus))
        {
            return InteractionErrors.ArticleInteractionTargetProjection.InvalidSourceStatus;
        }

        if (request.SourceVersion < 0)
        {
            return InteractionErrors.ArticleInteractionTargetProjection.InvalidSourceVersion;
        }

        if (string.IsNullOrWhiteSpace(request.SourceMessageId))
        {
            return InteractionErrors.ArticleInteractionTargetProjection.InvalidSourceMessageId;
        }

        if (request.SourceMessageId.Trim().Length != MessageIdLength)
        {
            return InteractionErrors.ArticleInteractionTargetProjection.InvalidSourceMessageId;
        }

        if (request.SourceOccurredAtUtc.HasValue &&
            request.SourceOccurredAtUtc.Value == default)
        {
            return InteractionErrors.ArticleInteractionTargetProjection.InvalidSourceOccurredAtUtc;
        }

        return null;
    }

    public static string NormalizeArticlePublicId(string articlePublicId)
    {
        return articlePublicId.Trim();
    }

    public static string NormalizeSourceStatus(string sourceStatus)
    {
        return sourceStatus.Trim();
    }

    public static string NormalizeSourceMessageId(string sourceMessageId)
    {
        return sourceMessageId.Trim();
    }
}