using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class ArticleInteractionStats
{
    public long ArticleInteractionStatsId { get; private set; }

    public string ArticlePublicId { get; private set; } = string.Empty;

    public long ViewCount { get; private set; }
    public long LikeCount { get; private set; }
    public long VisibleCommentCount { get; private set; }

    public long StatsVersion { get; private set; }

    public DateTime? LastMaterializedAtUtc { get; private set; }

    public string? LastPublishedMessageId { get; private set; }
    public DateTime? LastPublishedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ArticleInteractionStats()
    {
    }

    /// <summary>
    /// Rehydrates a persisted public counter snapshot owned by Interaction.
    /// Snapshot creation and updates are performed by the authoritative
    /// materialization procedure, not by application-side mutation.
    /// </summary>
    public static ArticleInteractionStats Rehydrate(
        long articleInteractionStatsId,
        string articlePublicId,
        long viewCount,
        long likeCount,
        long visibleCommentCount,
        long statsVersion,
        DateTime? lastMaterializedAtUtc,
        string? lastPublishedMessageId,
        DateTime? lastPublishedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ValidateId(articleInteractionStatsId);
        ValidateArticlePublicId(articlePublicId);
        ValidateViewCount(viewCount);
        ValidateLikeCount(likeCount);
        ValidateVisibleCommentCount(visibleCommentCount);
        ValidateStatsVersion(statsVersion);

        ValidateTimestampState(
            lastMaterializedAtUtc,
            lastPublishedAtUtc,
            createdAtUtc,
            updatedAtUtc);

        ValidatePublicationState(
            lastPublishedMessageId,
            lastPublishedAtUtc);

        return new ArticleInteractionStats
        {
            ArticleInteractionStatsId = articleInteractionStatsId,
            ArticlePublicId = NormalizeRequired(articlePublicId),
            ViewCount = viewCount,
            LikeCount = likeCount,
            VisibleCommentCount = visibleCommentCount,
            StatsVersion = statsVersion,
            LastMaterializedAtUtc = lastMaterializedAtUtc,
            LastPublishedMessageId = NormalizeOptional(lastPublishedMessageId),
            LastPublishedAtUtc = lastPublishedAtUtc,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    public bool HasPublishedSnapshot()
    {
        return !string.IsNullOrWhiteSpace(LastPublishedMessageId)
               && LastPublishedAtUtc.HasValue;
    }

    private static void ValidateId(long articleInteractionStatsId)
    {
        if (articleInteractionStatsId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_ID",
                "Article interaction stats id must be greater than zero.");
        }
    }

    private static void ValidateArticlePublicId(string articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.");
        }
    }

    private static void ValidateViewCount(long viewCount)
    {
        if (viewCount < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_VIEW_COUNT",
                "View count must be greater than or equal to zero.");
        }
    }

    private static void ValidateLikeCount(long likeCount)
    {
        if (likeCount < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_LIKE_COUNT",
                "Like count must be greater than or equal to zero.");
        }
    }

    private static void ValidateVisibleCommentCount(long visibleCommentCount)
    {
        if (visibleCommentCount < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_VISIBLE_COMMENT_COUNT",
                "Visible comment count must be greater than or equal to zero.");
        }
    }

    private static void ValidateStatsVersion(long statsVersion)
    {
        if (statsVersion < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_STATS_VERSION",
                "Stats version must be greater than or equal to zero.");
        }
    }

    private static void ValidateTimestampState(
        DateTime? lastMaterializedAtUtc,
        DateTime? lastPublishedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        if (createdAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_CREATED_AT_UTC",
                "CreatedAtUtc must be a valid datetime.");
        }

        if (lastMaterializedAtUtc.HasValue &&
            lastMaterializedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_LAST_MATERIALIZED_AT_UTC_ORDER",
                "LastMaterializedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        if (lastPublishedAtUtc.HasValue &&
            lastPublishedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_LAST_PUBLISHED_AT_UTC_ORDER",
                "LastPublishedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        if (updatedAtUtc.HasValue &&
            updatedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_UPDATED_AT_UTC_ORDER",
                "UpdatedAtUtc must be greater than or equal to CreatedAtUtc.");
        }
    }

    private static void ValidatePublicationState(
        string? lastPublishedMessageId,
        DateTime? lastPublishedAtUtc)
    {
        var hasMessageId = !string.IsNullOrWhiteSpace(lastPublishedMessageId);
        var hasPublishedAt = lastPublishedAtUtc.HasValue;

        if (hasMessageId != hasPublishedAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_STATS_INVALID_PUBLICATION_STATE",
                "LastPublishedMessageId and LastPublishedAtUtc must both be provided or both be null.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}