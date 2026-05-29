using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class ArticleViewCount
{
    public long ArticleViewCountId { get; private set; }

    public string ArticlePublicId { get; private set; } = string.Empty;

    public long ViewCount { get; private set; }
    public long ViewVersion { get; private set; }

    public DateTime? LastAcceptedViewAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ArticleViewCount()
    {
    }

    /// <summary>
    /// Rehydrates durable accepted-view count state persisted by Interaction.
    /// New accepted views are incremented atomically through the database
    /// procedure, not by mutating this entity in application memory.
    /// </summary>
    public static ArticleViewCount Rehydrate(
        long articleViewCountId,
        string articlePublicId,
        long viewCount,
        long viewVersion,
        DateTime? lastAcceptedViewAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ValidateId(articleViewCountId);
        ValidateArticlePublicId(articlePublicId);
        ValidateViewCount(viewCount);
        ValidateViewVersion(viewVersion);
        ValidateTimestampState(
            lastAcceptedViewAtUtc,
            createdAtUtc,
            updatedAtUtc);

        return new ArticleViewCount
        {
            ArticleViewCountId = articleViewCountId,
            ArticlePublicId = NormalizeRequired(articlePublicId),
            ViewCount = viewCount,
            ViewVersion = viewVersion,
            LastAcceptedViewAtUtc = lastAcceptedViewAtUtc,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private static void ValidateId(long articleViewCountId)
    {
        if (articleViewCountId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_ID",
                "Article view count id must be greater than zero.");
        }
    }

    private static void ValidateArticlePublicId(string articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.");
        }
    }

    private static void ValidateViewCount(long viewCount)
    {
        if (viewCount < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_VIEW_COUNT",
                "View count must be greater than or equal to zero.");
        }
    }

    private static void ValidateViewVersion(long viewVersion)
    {
        if (viewVersion < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_VIEW_VERSION",
                "View version must be greater than or equal to zero.");
        }
    }

    private static void ValidateTimestampState(
        DateTime? lastAcceptedViewAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        if (createdAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_CREATED_AT_UTC",
                "CreatedAtUtc must be a valid datetime.");
        }

        if (lastAcceptedViewAtUtc.HasValue &&
            lastAcceptedViewAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_LAST_ACCEPTED_VIEW_AT_UTC_ORDER",
                "LastAcceptedViewAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        if (updatedAtUtc.HasValue &&
            updatedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_COUNT_INVALID_UPDATED_AT_UTC_ORDER",
                "UpdatedAtUtc must be greater than or equal to CreatedAtUtc.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }
}