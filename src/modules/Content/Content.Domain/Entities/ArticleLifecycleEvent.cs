using Content.Domain.Common;
using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class ArticleLifecycleEvent
    {
        private ArticleLifecycleEvent(
            long eventId,
            long articleId,
            long articleVersion,
            string actionType,
            string? fromStatus,
            string? toStatus,
            string? reason,
            long actorUserId,
            DateTime occurredAt,
            string? correlationId,
            string? metadataJson)
        {
            EventId = eventId;
            ArticleId = articleId;
            ArticleVersion = articleVersion;
            ActionType = actionType;
            FromStatus = fromStatus;
            ToStatus = toStatus;
            Reason = reason;
            ActorUserId = actorUserId;
            OccurredAt = occurredAt;
            CorrelationId = correlationId;
            MetadataJson = metadataJson;
        }

        public long EventId { get; private set; }

        public long ArticleId { get; private set; }

        public long ArticleVersion { get; private set; }

        public string ActionType { get; private set; }

        public string? FromStatus { get; private set; }

        public string? ToStatus { get; private set; }

        public string? Reason { get; private set; }

        public long ActorUserId { get; private set; }

        public DateTime OccurredAt { get; private set; }

        public string? CorrelationId { get; private set; }

        public string? MetadataJson { get; private set; }

        public static ArticleLifecycleEvent Create(
            long articleId,
            long articleVersion,
            string actionType,
            string? fromStatus,
            string? toStatus,
            string? reason,
            long actorUserId,
            DateTime occurredAt,
            string? correlationId,
            string? metadataJson)
        {
            ValidateArticleId(articleId);
            ValidateArticleVersion(articleVersion);

            string normalizedActionType = ValidateAndNormalizeActionType(actionType);
            string? normalizedFromStatus = ValidateAndNormalizeStatus(fromStatus, nameof(fromStatus));
            string? normalizedToStatus = ValidateAndNormalizeStatus(toStatus, nameof(toStatus));

            ValidateReason(normalizedActionType, reason);
            ValidateActorUserId(actorUserId);
            ValidateCorrelationId(correlationId);

            return new ArticleLifecycleEvent(
                eventId: 0,
                articleId: articleId,
                articleVersion: articleVersion,
                actionType: normalizedActionType,
                fromStatus: normalizedFromStatus,
                toStatus: normalizedToStatus,
                reason: ContentText.NormalizeOptional(reason),
                actorUserId: actorUserId,
                occurredAt: occurredAt,
                correlationId: ContentText.NormalizeOptional(correlationId),
                metadataJson: ContentText.NormalizeOptional(metadataJson));
        }

        public static ArticleLifecycleEvent Rehydrate(
            long eventId,
            long articleId,
            long articleVersion,
            string actionType,
            string? fromStatus,
            string? toStatus,
            string? reason,
            long actorUserId,
            DateTime occurredAt,
            string? correlationId,
            string? metadataJson)
        {
            ContentGuard.AgainstInvalidId(
                eventId,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_EVENT_ID",
                "Lifecycle event id must be greater than zero.");

            ValidateArticleId(articleId);
            ValidateArticleVersion(articleVersion);

            string normalizedActionType = ValidateAndNormalizeActionType(actionType);
            string? normalizedFromStatus = ValidateAndNormalizeStatus(fromStatus, nameof(fromStatus));
            string? normalizedToStatus = ValidateAndNormalizeStatus(toStatus, nameof(toStatus));

            ValidateReason(normalizedActionType, reason);
            ValidateActorUserId(actorUserId);
            ValidateCorrelationId(correlationId);

            return new ArticleLifecycleEvent(
                eventId: eventId,
                articleId: articleId,
                articleVersion: articleVersion,
                actionType: normalizedActionType,
                fromStatus: normalizedFromStatus,
                toStatus: normalizedToStatus,
                reason: ContentText.NormalizeOptional(reason),
                actorUserId: actorUserId,
                occurredAt: occurredAt,
                correlationId: ContentText.NormalizeOptional(correlationId),
                metadataJson: ContentText.NormalizeOptional(metadataJson));
        }

        public bool IsPublish()
        {
            return ActionType == ArticleLifecycleActionTypes.Publish;
        }

        public bool IsUnpublish()
        {
            return ActionType == ArticleLifecycleActionTypes.Unpublish;
        }

        public bool IsArchive()
        {
            return ActionType == ArticleLifecycleActionTypes.Archive;
        }

        public bool IsSoftDelete()
        {
            return ActionType == ArticleLifecycleActionTypes.SoftDelete;
        }

        private static void ValidateArticleId(long articleId)
        {
            ContentGuard.AgainstInvalidId(
                articleId,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }

        private static void ValidateArticleVersion(long articleVersion)
        {
            ContentGuard.AgainstInvalidVersion(
                articleVersion,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION",
                "Article version must be greater than zero.");
        }

        private static string ValidateAndNormalizeActionType(string actionType)
        {
            ContentGuard.AgainstRequiredText(
                actionType,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_REQUIRED",
                "Lifecycle action type is required.");

            string trimmed = actionType.Trim();

            ContentGuard.AgainstTooLong(
                trimmed,
                ContentFieldLimits.LifecycleActionTypeMaxLength,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_TOO_LONG",
                $"Lifecycle action type must not exceed {ContentFieldLimits.LifecycleActionTypeMaxLength} characters.");

            try
            {
                return ArticleLifecycleActionTypes.Normalize(trimmed);
            }
            catch (ContentDomainException)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID",
                    "Lifecycle action type is invalid.");
            }
        }

        private static string? ValidateAndNormalizeStatus(
            string? status,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            string trimmed = status.Trim();

            ContentGuard.AgainstTooLong(
                trimmed,
                ContentFieldLimits.LifecycleStatusMaxLength,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_TOO_LONG",
                $"{fieldName} must not exceed {ContentFieldLimits.LifecycleStatusMaxLength} characters.");

            try
            {
                return ArticleStatuses.Normalize(trimmed);
            }
            catch (ContentDomainException)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID",
                    $"{fieldName} is invalid.");
            }
        }

        private static void ValidateReason(
            string actionType,
            string? reason)
        {
            ContentGuard.AgainstTooLong(
                reason,
                ContentFieldLimits.LifecycleReasonMaxLength,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_REASON_TOO_LONG",
                $"Reason must not exceed {ContentFieldLimits.LifecycleReasonMaxLength} characters.");

            if (actionType == ArticleLifecycleActionTypes.Unpublish &&
                string.IsNullOrWhiteSpace(reason))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_UNPUBLISH_REASON_REQUIRED",
                    "Reason is required for unpublish lifecycle event.");
            }
        }

        private static void ValidateActorUserId(long actorUserId)
        {
            ContentGuard.AgainstInvalidId(
                actorUserId,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID",
                "Actor user id must be greater than zero.");
        }

        private static void ValidateCorrelationId(string? correlationId)
        {
            ContentGuard.AgainstTooLong(
                correlationId,
                ContentFieldLimits.CorrelationIdMaxLength,
                "CONTENT.ARTICLE_LIFECYCLE_EVENT_CORRELATION_ID_TOO_LONG",
                $"Correlation id must not exceed {ContentFieldLimits.CorrelationIdMaxLength} characters.");
        }
    }
}