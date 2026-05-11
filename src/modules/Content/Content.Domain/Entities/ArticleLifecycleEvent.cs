using Content.Domain.Constants;
using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class ArticleLifecycleEvent
    {
        private const int ActionTypeMaxLength = 30;
        private const int StatusMaxLength = 30;
        private const int ReasonMaxLength = 500;
        private const int CorrelationIdMaxLength = 100;

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
            ValidateActionType(actionType);
            ValidateStatus(fromStatus, nameof(fromStatus));
            ValidateStatus(toStatus, nameof(toStatus));
            ValidateReason(actionType, reason);
            ValidateActorUserId(actorUserId);
            ValidateCorrelationId(correlationId);

            return new ArticleLifecycleEvent(
                eventId: 0,
                articleId: articleId,
                articleVersion: articleVersion,
                actionType: actionType.Trim(),
                fromStatus: NormalizeOptional(fromStatus),
                toStatus: NormalizeOptional(toStatus),
                reason: NormalizeOptional(reason),
                actorUserId: actorUserId,
                occurredAt: occurredAt,
                correlationId: NormalizeOptional(correlationId),
                metadataJson: NormalizeOptional(metadataJson));
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
            if (eventId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_EVENT_ID",
                    "Lifecycle event id must be greater than zero.");
            }

            ValidateArticleId(articleId);
            ValidateArticleVersion(articleVersion);
            ValidateActionType(actionType);
            ValidateStatus(fromStatus, nameof(fromStatus));
            ValidateStatus(toStatus, nameof(toStatus));
            ValidateReason(actionType, reason);
            ValidateActorUserId(actorUserId);
            ValidateCorrelationId(correlationId);

            return new ArticleLifecycleEvent(
                eventId: eventId,
                articleId: articleId,
                articleVersion: articleVersion,
                actionType: actionType.Trim(),
                fromStatus: NormalizeOptional(fromStatus),
                toStatus: NormalizeOptional(toStatus),
                reason: NormalizeOptional(reason),
                actorUserId: actorUserId,
                occurredAt: occurredAt,
                correlationId: NormalizeOptional(correlationId),
                metadataJson: NormalizeOptional(metadataJson));
        }

        public bool IsPublish()
        {
            return ActionType.Equals(
                ArticleLifecycleActionTypes.Publish,
                StringComparison.OrdinalIgnoreCase);
        }

        public bool IsUnpublish()
        {
            return ActionType.Equals(
                ArticleLifecycleActionTypes.Unpublish,
                StringComparison.OrdinalIgnoreCase);
        }

        public bool IsArchive()
        {
            return ActionType.Equals(
                ArticleLifecycleActionTypes.Archive,
                StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSoftDelete()
        {
            return ActionType.Equals(
                ArticleLifecycleActionTypes.SoftDelete,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateArticleId(long articleId)
        {
            if (articleId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.");
            }
        }

        private static void ValidateArticleVersion(long articleVersion)
        {
            if (articleVersion <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ARTICLE_VERSION",
                    "Article version must be greater than zero.");
            }
        }

        private static void ValidateActionType(string actionType)
        {
            if (string.IsNullOrWhiteSpace(actionType))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_REQUIRED",
                    "Lifecycle action type is required.");
            }

            string trimmed = actionType.Trim();

            if (trimmed.Length > ActionTypeMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_TOO_LONG",
                    $"Lifecycle action type must not exceed {ActionTypeMaxLength} characters.");
            }

            if (!ArticleLifecycleActionTypes.IsValid(trimmed))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_ACTION_TYPE_INVALID",
                    "Lifecycle action type is invalid.");
            }
        }

        private static void ValidateStatus(string? status, string fieldName)
        {
            if (status is null)
            {
                return;
            }

            string trimmed = status.Trim();

            if (trimmed.Length == 0)
            {
                return;
            }

            if (trimmed.Length > StatusMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_TOO_LONG",
                    $"{fieldName} must not exceed {StatusMaxLength} characters.");
            }

            if (!ArticleStatuses.IsValid(trimmed))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_STATUS_INVALID",
                    $"{fieldName} is invalid.");
            }
        }

        private static void ValidateReason(string actionType, string? reason)
        {
            if (reason is not null && reason.Trim().Length > ReasonMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_REASON_TOO_LONG",
                    $"Reason must not exceed {ReasonMaxLength} characters.");
            }

            if (actionType.Equals(
                    ArticleLifecycleActionTypes.Unpublish,
                    StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(reason))
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_UNPUBLISH_REASON_REQUIRED",
                    "Reason is required for unpublish lifecycle event.");
            }
        }

        private static void ValidateActorUserId(long actorUserId)
        {
            if (actorUserId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_INVALID_ACTOR_USER_ID",
                    "Actor user id must be greater than zero.");
            }
        }

        private static void ValidateCorrelationId(string? correlationId)
        {
            if (correlationId is not null &&
                correlationId.Trim().Length > CorrelationIdMaxLength)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_LIFECYCLE_EVENT_CORRELATION_ID_TOO_LONG",
                    $"Correlation id must not exceed {CorrelationIdMaxLength} characters.");
            }
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}