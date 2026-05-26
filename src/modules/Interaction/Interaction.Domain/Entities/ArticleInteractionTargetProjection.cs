using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class ArticleInteractionTargetProjection
{
    public long ArticleInteractionTargetProjectionId { get; private set; }

    public string ArticlePublicId { get; private set; } = string.Empty;

    public string SourceStatus { get; private set; } = string.Empty;
    public bool IsInteractionEnabled { get; private set; }

    public long LastSourceVersion { get; private set; }
    public string? LastSourceMessageId { get; private set; }
    public DateTime? LastSourceOccurredAtUtc { get; private set; }

    public DateTime LastSyncedAtUtc { get; private set; }

    /// <summary>
    /// Reserved for future gap-detection and resync workflow.
    /// The initial Interaction V1 implementation maps this persisted value
    /// but does not expose a recovery/resync use case yet.
    /// </summary>
    public bool RequiresResync { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ArticleInteractionTargetProjection()
    {
    }

    /// <summary>
    /// Rehydrates Interaction-owned eligibility projection state persisted
    /// from Content-derived asynchronous input.
    /// </summary>
    public static ArticleInteractionTargetProjection Rehydrate(
        long articleInteractionTargetProjectionId,
        string articlePublicId,
        string sourceStatus,
        bool isInteractionEnabled,
        long lastSourceVersion,
        string? lastSourceMessageId,
        DateTime? lastSourceOccurredAtUtc,
        DateTime lastSyncedAtUtc,
        bool requiresResync,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        ValidateId(articleInteractionTargetProjectionId);
        ValidateArticlePublicId(articlePublicId);
        ValidateSourceStatus(sourceStatus);
        ValidateLastSourceVersion(lastSourceVersion);
        ValidateOptionalMessageId(lastSourceMessageId);
        ValidateTimestampState(
            lastSyncedAtUtc,
            createdAtUtc,
            updatedAtUtc);

        return new ArticleInteractionTargetProjection
        {
            ArticleInteractionTargetProjectionId = articleInteractionTargetProjectionId,
            ArticlePublicId = NormalizeRequired(articlePublicId),
            SourceStatus = NormalizeRequired(sourceStatus),
            IsInteractionEnabled = isInteractionEnabled,
            LastSourceVersion = lastSourceVersion,
            LastSourceMessageId = NormalizeOptional(lastSourceMessageId),
            LastSourceOccurredAtUtc = lastSourceOccurredAtUtc,
            LastSyncedAtUtc = lastSyncedAtUtc,
            RequiresResync = requiresResync,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    /// <summary>
    /// Initial V1 local eligibility gate.
    /// RequiresResync handling is intentionally deferred from the current phase.
    /// </summary>
    public bool AllowsNewInteraction()
    {
        return IsInteractionEnabled;
    }

    private static void ValidateId(long articleInteractionTargetProjectionId)
    {
        if (articleInteractionTargetProjectionId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_INVALID_ID",
                "Article interaction target projection id must be greater than zero.");
        }
    }

    private static void ValidateArticlePublicId(string articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.");
        }
    }

    private static void ValidateSourceStatus(string sourceStatus)
    {
        if (string.IsNullOrWhiteSpace(sourceStatus))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_SOURCE_STATUS_REQUIRED",
                "Source status is required.");
        }
    }

    private static void ValidateLastSourceVersion(long lastSourceVersion)
    {
        if (lastSourceVersion < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_INVALID_SOURCE_VERSION",
                "Last source version must be greater than or equal to zero.");
        }
    }

    private static void ValidateOptionalMessageId(string? lastSourceMessageId)
    {
        if (lastSourceMessageId is not null &&
            string.IsNullOrWhiteSpace(lastSourceMessageId))
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_INVALID_SOURCE_MESSAGE_ID",
                "Last source message id must not be blank when provided.");
        }
    }

    private static void ValidateTimestampState(
        DateTime lastSyncedAtUtc,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
    {
        if (lastSyncedAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_INVALID_LAST_SYNCED_AT_UTC",
                "LastSyncedAtUtc must be a valid datetime.");
        }

        if (createdAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_INVALID_CREATED_AT_UTC",
                "CreatedAtUtc must be a valid datetime.");
        }

        if (updatedAtUtc.HasValue && updatedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_INTERACTION_TARGET_PROJECTION_INVALID_UPDATED_AT_UTC_ORDER",
                "UpdatedAtUtc must be greater than or equal to CreatedAtUtc.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return value?.Trim();
    }
}