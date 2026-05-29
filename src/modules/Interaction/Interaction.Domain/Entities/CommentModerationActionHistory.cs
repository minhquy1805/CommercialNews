using Interaction.Domain.Constants;
using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class CommentModerationActionHistory
{
    public long CommentModerationActionHistoryId { get; private set; }

    public string PublicId { get; private set; } = string.Empty;

    public long CommentId { get; private set; }
    public long? CommentModerationCaseId { get; private set; }

    public string ActionType { get; private set; } = string.Empty;

    public string? FromStatus { get; private set; }
    public string? ToStatus { get; private set; }

    public long? ActorUserId { get; private set; }
    public string ActorType { get; private set; } = string.Empty;

    public string? ReasonCode { get; private set; }
    public string? Note { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    private CommentModerationActionHistory()
    {
    }

    /// <summary>
    /// Rehydrates a persisted local moderation-history record.
    /// History rows are created by authoritative moderation workflow procedures,
    /// not independently by application code.
    /// </summary>
    public static CommentModerationActionHistory Rehydrate(
        long commentModerationActionHistoryId,
        string publicId,
        long commentId,
        long? commentModerationCaseId,
        string actionType,
        string? fromStatus,
        string? toStatus,
        long? actorUserId,
        string actorType,
        string? reasonCode,
        string? note,
        DateTime occurredAtUtc,
        string? correlationId)
    {
        ValidateId(commentModerationActionHistoryId);
        ValidatePublicId(publicId);
        ValidateCommentId(commentId);
        ValidateCommentModerationCaseId(commentModerationCaseId);
        ValidateActionType(actionType);
        ValidateOptionalCommentStatus(fromStatus, nameof(fromStatus));
        ValidateOptionalCommentStatus(toStatus, nameof(toStatus));
        ValidateActorUserId(actorUserId);
        ValidateActorType(actorType);
        ValidateOptionalReasonCode(reasonCode);
        ValidateOptionalNote(note);
        ValidateOccurredAtUtc(occurredAtUtc);
        ValidateOptionalCorrelationId(correlationId);

        ValidateActionState(
            actionType,
            commentModerationCaseId,
            fromStatus,
            toStatus,
            reasonCode,
            note);

        return new CommentModerationActionHistory
        {
            CommentModerationActionHistoryId = commentModerationActionHistoryId,
            PublicId = NormalizeRequired(publicId),
            CommentId = commentId,
            CommentModerationCaseId = commentModerationCaseId,
            ActionType = NormalizeRequired(actionType),
            FromStatus = NormalizeOptional(fromStatus),
            ToStatus = NormalizeOptional(toStatus),
            ActorUserId = actorUserId,
            ActorType = NormalizeRequired(actorType),
            ReasonCode = NormalizeOptional(reasonCode),
            Note = NormalizeOptional(note),
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = NormalizeOptional(correlationId)
        };
    }

    public bool BelongsToModerationCase()
    {
        return CommentModerationCaseId.HasValue;
    }

    private static void ValidateId(long commentModerationActionHistoryId)
    {
        if (commentModerationActionHistoryId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_ID",
                "Comment moderation action history id must be greater than zero.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_PUBLIC_ID_REQUIRED",
                "Comment moderation action history public id is required.");
        }
    }

    private static void ValidateCommentId(long commentId)
    {
        if (commentId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_COMMENT_ID",
                "Comment id must be greater than zero.");
        }
    }

    private static void ValidateCommentModerationCaseId(long? commentModerationCaseId)
    {
        if (commentModerationCaseId.HasValue && commentModerationCaseId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_CASE_ID",
                "Comment moderation case id must be greater than zero when provided.");
        }
    }

    private static void ValidateActionType(string actionType)
    {
        if (!CommentModerationActionTypes.IsValid(actionType))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_ACTION_TYPE",
                "Comment moderation action type is invalid.");
        }
    }

    private static void ValidateOptionalCommentStatus(
        string? status,
        string propertyName)
    {
        if (status is not null && !CommentStatuses.IsValid(status))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_COMMENT_STATUS",
                $"{propertyName} is not a valid comment status.");
        }
    }

    private static void ValidateActorUserId(long? actorUserId)
    {
        if (actorUserId.HasValue && actorUserId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_ACTOR_USER_ID",
                "Actor user id must be greater than zero when provided.");
        }
    }

    private static void ValidateActorType(string actorType)
    {
        if (string.IsNullOrWhiteSpace(actorType))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_ACTOR_TYPE_REQUIRED",
                "Actor type is required.");
        }

        if (actorType.Trim().Length > 30)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_ACTOR_TYPE_TOO_LONG",
                "Actor type must not exceed 30 characters.");
        }
    }

    private static void ValidateOptionalReasonCode(string? reasonCode)
    {
        if (reasonCode is not null && !ModerationReasonCodes.IsValid(reasonCode))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_REASON_CODE",
                "Comment moderation reason code is invalid.");
        }
    }

    private static void ValidateOptionalNote(string? note)
    {
        if (note is not null && string.IsNullOrWhiteSpace(note))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_NOTE",
                "Moderation note must not be blank when provided.");
        }

        if (note?.Trim().Length > 1000)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_NOTE_TOO_LONG",
                "Moderation note must not exceed 1000 characters.");
        }
    }

    private static void ValidateOccurredAtUtc(DateTime occurredAtUtc)
    {
        if (occurredAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_OCCURRED_AT_UTC",
                "OccurredAtUtc must be a valid datetime.");
        }
    }

    private static void ValidateOptionalCorrelationId(string? correlationId)
    {
        if (correlationId is not null && string.IsNullOrWhiteSpace(correlationId))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_CORRELATION_ID",
                "Correlation id must not be blank when provided.");
        }
    }

    private static void ValidateActionState(
        string actionType,
        long? commentModerationCaseId,
        string? fromStatus,
        string? toStatus,
        string? reasonCode,
        string? note)
    {
        if (EqualsAction(actionType, CommentModerationActionTypes.Hide))
        {
            EnsureNoCase(commentModerationCaseId);
            EnsureTransition(fromStatus, CommentStatuses.Visible, toStatus, CommentStatuses.Hidden);
            EnsureReasonIsProvided(reasonCode, note);
            return;
        }

        if (EqualsAction(actionType, CommentModerationActionTypes.Restore))
        {
            EnsureNoCase(commentModerationCaseId);
            EnsureTransition(fromStatus, CommentStatuses.Hidden, toStatus, CommentStatuses.Visible);
            return;
        }

        if (EqualsAction(actionType, CommentModerationActionTypes.DismissReportedCase))
        {
            EnsureCaseIsProvided(commentModerationCaseId);
            EnsureTransition(fromStatus, CommentStatuses.Visible, toStatus, CommentStatuses.Visible);
            EnsureReasonIsProvided(reasonCode, note);
            return;
        }

        if (EqualsAction(actionType, CommentModerationActionTypes.HideReportedComment))
        {
            EnsureCaseIsProvided(commentModerationCaseId);
            EnsureTransition(fromStatus, CommentStatuses.Visible, toStatus, CommentStatuses.Hidden);
            EnsureReasonIsProvided(reasonCode, note);
            return;
        }

        if (EqualsAction(actionType, CommentModerationActionTypes.CloseCaseByAuthorDeletion))
        {
            EnsureCaseIsProvided(commentModerationCaseId);
            EnsureToStatus(toStatus, CommentStatuses.Deleted);

            if (reasonCode is not null || note is not null)
            {
                throw new InteractionDomainException(
                    "INTERACTION.COMMENT_MODERATION_HISTORY_AUTHOR_DELETE_REASON_INVALID",
                    "CloseCaseByAuthorDeletion must not contain a reason code or note.");
            }

            return;
        }

        // Reserved for future selective moderation.
        if (EqualsAction(actionType, CommentModerationActionTypes.Approve))
        {
            EnsureNoCase(commentModerationCaseId);
            EnsureTransition(fromStatus, CommentStatuses.Pending, toStatus, CommentStatuses.Visible);
            return;
        }

        // Reserved for future selective moderation.
        if (EqualsAction(actionType, CommentModerationActionTypes.Reject))
        {
            EnsureNoCase(commentModerationCaseId);
            EnsureTransition(fromStatus, CommentStatuses.Pending, toStatus, CommentStatuses.Rejected);
            EnsureReasonIsProvided(reasonCode, note);
        }
    }

    private static void EnsureCaseIsProvided(long? commentModerationCaseId)
    {
        if (!commentModerationCaseId.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_CASE_REQUIRED",
                "Comment moderation case id is required for this action type.");
        }
    }

    private static void EnsureNoCase(long? commentModerationCaseId)
    {
        if (commentModerationCaseId.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_CASE_NOT_ALLOWED",
                "Comment moderation case id is not allowed for this action type.");
        }
    }

    private static void EnsureTransition(
        string? actualFromStatus,
        string expectedFromStatus,
        string? actualToStatus,
        string expectedToStatus)
    {
        if (!string.Equals(actualFromStatus, expectedFromStatus, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(actualToStatus, expectedToStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_TRANSITION",
                "Comment moderation history transition does not match its action type.");
        }
    }

    private static void EnsureToStatus(
        string? actualToStatus,
        string expectedToStatus)
    {
        if (!string.Equals(actualToStatus, expectedToStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_INVALID_TO_STATUS",
                "Comment moderation history target status does not match its action type.");
        }
    }

    private static void EnsureReasonIsProvided(
        string? reasonCode,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_REASON_REQUIRED",
                "Reason code is required for this moderation action.");
        }

        if (string.Equals(reasonCode, ModerationReasonCodes.Other, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(note))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_HISTORY_OTHER_NOTE_REQUIRED",
                "Note is required when moderation reason code is Other.");
        }
    }

    private static bool EqualsAction(string actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
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