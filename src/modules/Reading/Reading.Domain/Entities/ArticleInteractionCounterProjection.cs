using Reading.Domain.Exceptions;

namespace Reading.Domain.Entities;

public sealed class ArticleInteractionCounterProjection
{
    private const int PublicIdLength = 26;
    private const int MessageIdLength = 26;

    private ArticleInteractionCounterProjection()
    {
    }

    public string ArticlePublicId { get; private set; } = string.Empty;

    public long ViewCount { get; private set; }

    public long LikeCount { get; private set; }

    public long VisibleCommentCount { get; private set; }

    /// <summary>
    /// Version of the Interaction-owned public counter snapshot.
    /// This is not a Content, SEO, Media, or Identity source version.
    /// </summary>
    public long InteractionStatsVersion { get; private set; }

    /// <summary>
    /// Message id of the latest applied Interaction counter snapshot event.
    /// </summary>
    public string LastEventMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp at which the source Interaction event occurred.
    /// </summary>
    public DateTime LastSourceOccurredAtUtc { get; private set; }

    /// <summary>
    /// Timestamp at which Reading last synchronized this projection.
    /// </summary>
    public DateTime LastSyncedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static ArticleInteractionCounterProjection Create(
        string articlePublicId,
        long viewCount,
        long likeCount,
        long visibleCommentCount,
        long interactionStatsVersion,
        string lastEventMessageId,
        DateTime lastSourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateArticlePublicId(articlePublicId);
        ValidateCounters(viewCount, likeCount, visibleCommentCount);
        ValidateInteractionStatsVersion(interactionStatsVersion);
        ValidateRequiredMessageId(lastEventMessageId);

        ValidateRequiredTimestamp(
            lastSourceOccurredAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_SOURCE_OCCURRED_AT_UTC",
            "Interaction counter source occurred timestamp is required.");

        ValidateRequiredTimestamp(
            syncedAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_LAST_SYNCED_AT_UTC",
            "Interaction counter last synced timestamp is required.");

        return new ArticleInteractionCounterProjection
        {
            ArticlePublicId = articlePublicId.Trim(),
            ViewCount = viewCount,
            LikeCount = likeCount,
            VisibleCommentCount = visibleCommentCount,
            InteractionStatsVersion = interactionStatsVersion,
            LastEventMessageId = lastEventMessageId.Trim(),
            LastSourceOccurredAtUtc = lastSourceOccurredAtUtc,
            LastSyncedAtUtc = syncedAtUtc,
            CreatedAtUtc = syncedAtUtc,
            UpdatedAtUtc = syncedAtUtc
        };
    }

    public static ArticleInteractionCounterProjection Rehydrate(
        string articlePublicId,
        long viewCount,
        long likeCount,
        long visibleCommentCount,
        long interactionStatsVersion,
        string lastEventMessageId,
        DateTime lastSourceOccurredAtUtc,
        DateTime lastSyncedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ValidateArticlePublicId(articlePublicId);
        ValidateCounters(viewCount, likeCount, visibleCommentCount);
        ValidateInteractionStatsVersion(interactionStatsVersion);
        ValidateRequiredMessageId(lastEventMessageId);

        ValidateRequiredTimestamp(
            lastSourceOccurredAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_SOURCE_OCCURRED_AT_UTC",
            "Interaction counter source occurred timestamp is required.");

        ValidateRequiredTimestamp(
            lastSyncedAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_LAST_SYNCED_AT_UTC",
            "Interaction counter last synced timestamp is required.");

        ValidateRequiredTimestamp(
            createdAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_CREATED_AT_UTC",
            "Interaction counter created timestamp is required.");

        ValidateOptionalTimestamp(
            updatedAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_UPDATED_AT_UTC",
            "Interaction counter updated timestamp is invalid.");

        return new ArticleInteractionCounterProjection
        {
            ArticlePublicId = articlePublicId.Trim(),
            ViewCount = viewCount,
            LikeCount = likeCount,
            VisibleCommentCount = visibleCommentCount,
            InteractionStatsVersion = interactionStatsVersion,
            LastEventMessageId = lastEventMessageId.Trim(),
            LastSourceOccurredAtUtc = lastSourceOccurredAtUtc,
            LastSyncedAtUtc = lastSyncedAtUtc,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    public bool CanApply(long incomingInteractionStatsVersion)
    {
        ValidateInteractionStatsVersion(incomingInteractionStatsVersion);

        return incomingInteractionStatsVersion > InteractionStatsVersion;
    }

    public bool Apply(
        long viewCount,
        long likeCount,
        long visibleCommentCount,
        long interactionStatsVersion,
        string messageId,
        DateTime sourceOccurredAtUtc,
        DateTime syncedAtUtc)
    {
        ValidateCounters(viewCount, likeCount, visibleCommentCount);
        ValidateInteractionStatsVersion(interactionStatsVersion);
        ValidateRequiredMessageId(messageId);

        ValidateRequiredTimestamp(
            sourceOccurredAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_SOURCE_OCCURRED_AT_UTC",
            "Interaction counter source occurred timestamp is required.");

        ValidateRequiredTimestamp(
            syncedAtUtc,
            "READING.INVALID_INTERACTION_COUNTER_LAST_SYNCED_AT_UTC",
            "Interaction counter last synced timestamp is required.");

        if (!CanApply(interactionStatsVersion))
        {
            return false;
        }

        ViewCount = viewCount;
        LikeCount = likeCount;
        VisibleCommentCount = visibleCommentCount;
        InteractionStatsVersion = interactionStatsVersion;
        LastEventMessageId = messageId.Trim();
        LastSourceOccurredAtUtc = sourceOccurredAtUtc;
        LastSyncedAtUtc = syncedAtUtc;
        UpdatedAtUtc = syncedAtUtc;

        return true;
    }

    private static void ValidateArticlePublicId(string? articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId)
            || articlePublicId.Trim().Length != PublicIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INTERACTION_COUNTER_ARTICLE_PUBLIC_ID",
                "Interaction counter article public id must be a 26-character value.");
        }
    }

    private static void ValidateCounters(
        long viewCount,
        long likeCount,
        long visibleCommentCount)
    {
        if (viewCount < 0 || likeCount < 0 || visibleCommentCount < 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INTERACTION_COUNTERS",
                "Interaction counters must be non-negative.");
        }
    }

    private static void ValidateInteractionStatsVersion(
        long interactionStatsVersion)
    {
        if (interactionStatsVersion <= 0)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INTERACTION_STATS_VERSION",
                "Interaction stats version must be greater than zero.");
        }
    }

    private static void ValidateRequiredMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId)
            || messageId.Trim().Length != MessageIdLength)
        {
            throw new ReadingDomainException(
                "READING.INVALID_INTERACTION_COUNTER_MESSAGE_ID",
                "Interaction counter message id must be a 26-character value.");
        }
    }

    private static void ValidateRequiredTimestamp(
        DateTime timestamp,
        string code,
        string message)
    {
        if (timestamp == default)
        {
            throw new ReadingDomainException(code, message);
        }
    }

    private static void ValidateOptionalTimestamp(
        DateTime? timestamp,
        string code,
        string message)
    {
        if (timestamp == default(DateTime))
        {
            throw new ReadingDomainException(code, message);
        }
    }
}
