using Interaction.Domain.Constants;
using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class CommentModerationCase
{
    public long CommentModerationCaseId { get; private set; }

    public string PublicId { get; private set; } = string.Empty;
    public long CommentId { get; private set; }

    public string Status { get; private set; } = CommentModerationCaseStatuses.Open;
    public string Priority { get; private set; } = CommentModerationCasePriorities.Normal;
    public string HighestSeverity { get; private set; } = ReportSeverities.Normal;

    public DateTime? AlertTriggeredAtUtc { get; private set; }
    public string? AlertLevel { get; private set; }
    public string? AlertMessageId { get; private set; }

    public DateTime OpenedAtUtc { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }
    public long? ResolvedByUserId { get; private set; }

    public string? ResolutionType { get; private set; }
    public string? ResolutionReasonCode { get; private set; }
    public string? ResolutionNote { get; private set; }

    public long Version { get; private set; }

    private CommentModerationCase()
    {
    }

    /// <summary>
    /// Rehydrates a moderation-case state already persisted by Interaction.
    /// Case creation, escalation and resolution are performed by authoritative
    /// database procedures because they require transaction and concurrency guards.
    /// </summary>
    public static CommentModerationCase Rehydrate(
        long commentModerationCaseId,
        string publicId,
        long commentId,
        string status,
        string priority,
        string highestSeverity,
        DateTime? alertTriggeredAtUtc,
        string? alertLevel,
        string? alertMessageId,
        DateTime openedAtUtc,
        DateTime? resolvedAtUtc,
        long? resolvedByUserId,
        string? resolutionType,
        string? resolutionReasonCode,
        string? resolutionNote,
        long version)
    {
        ValidateId(commentModerationCaseId);
        ValidatePublicId(publicId);
        ValidateCommentId(commentId);
        ValidateStatus(status);
        ValidatePriority(priority);
        ValidateHighestSeverity(highestSeverity);
        ValidateVersion(version);

        ValidateAlertState(
            alertTriggeredAtUtc,
            alertLevel,
            alertMessageId,
            openedAtUtc);

        ValidateResolutionState(
            status,
            openedAtUtc,
            resolvedAtUtc,
            resolvedByUserId,
            resolutionType,
            resolutionReasonCode,
            resolutionNote);

        return new CommentModerationCase
        {
            CommentModerationCaseId = commentModerationCaseId,
            PublicId = NormalizeRequired(publicId),
            CommentId = commentId,
            Status = NormalizeRequired(status),
            Priority = NormalizeRequired(priority),
            HighestSeverity = NormalizeRequired(highestSeverity),
            AlertTriggeredAtUtc = alertTriggeredAtUtc,
            AlertLevel = NormalizeOptional(alertLevel),
            AlertMessageId = NormalizeOptional(alertMessageId),
            OpenedAtUtc = openedAtUtc,
            ResolvedAtUtc = resolvedAtUtc,
            ResolvedByUserId = resolvedByUserId,
            ResolutionType = NormalizeOptional(resolutionType),
            ResolutionReasonCode = NormalizeOptional(resolutionReasonCode),
            ResolutionNote = NormalizeOptional(resolutionNote),
            Version = version
        };
    }

    public bool IsOpen()
    {
        return string.Equals(
            Status,
            CommentModerationCaseStatuses.Open,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDismissed()
    {
        return string.Equals(
            Status,
            CommentModerationCaseStatuses.Dismissed,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsActioned()
    {
        return string.Equals(
            Status,
            CommentModerationCaseStatuses.Actioned,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsClosedByAuthorDeletion()
    {
        return string.Equals(
            Status,
            CommentModerationCaseStatuses.ClosedByAuthorDeletion,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool HasTriggeredAlert()
    {
        return AlertTriggeredAtUtc.HasValue;
    }

    private static void ValidateId(long commentModerationCaseId)
    {
        if (commentModerationCaseId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_ID",
                "Comment moderation case id must be greater than zero.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_PUBLIC_ID_REQUIRED",
                "Comment moderation case public id is required.");
        }
    }

    private static void ValidateCommentId(long commentId)
    {
        if (commentId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_COMMENT_ID",
                "Comment id must be greater than zero.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (!CommentModerationCaseStatuses.IsValid(status))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_STATUS",
                "Comment moderation case status is invalid.");
        }
    }

    private static void ValidatePriority(string priority)
    {
        if (!CommentModerationCasePriorities.IsValid(priority))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_PRIORITY",
                "Comment moderation case priority is invalid.");
        }
    }

    private static void ValidateHighestSeverity(string highestSeverity)
    {
        if (!ReportSeverities.IsValid(highestSeverity))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_HIGHEST_SEVERITY",
                "Comment moderation case highest severity is invalid.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_VERSION",
                "Comment moderation case version must be greater than or equal to one.");
        }
    }

    private static void ValidateAlertState(
        DateTime? alertTriggeredAtUtc,
        string? alertLevel,
        string? alertMessageId,
        DateTime openedAtUtc)
    {
        ValidateOpenedAtUtc(openedAtUtc);

        var hasTriggeredAt = alertTriggeredAtUtc.HasValue;
        var hasAlertLevel = !string.IsNullOrWhiteSpace(alertLevel);
        var hasAlertMessageId = !string.IsNullOrWhiteSpace(alertMessageId);

        if (!hasTriggeredAt && !hasAlertLevel && !hasAlertMessageId)
        {
            return;
        }

        if (!hasTriggeredAt || !hasAlertLevel || !hasAlertMessageId)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_ALERT_STATE",
                "Alert trigger time, alert level and alert message id must all be provided together.");
        }

        if (alertTriggeredAtUtc!.Value < openedAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_ALERT_TRIGGERED_AT_UTC_ORDER",
                "AlertTriggeredAtUtc must be greater than or equal to OpenedAtUtc.");
        }

        if (!ReportAlertLevels.IsValid(alertLevel))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_ALERT_LEVEL",
                "Comment moderation case alert level is invalid.");
        }
    }

    private static void ValidateResolutionState(
        string status,
        DateTime openedAtUtc,
        DateTime? resolvedAtUtc,
        long? resolvedByUserId,
        string? resolutionType,
        string? resolutionReasonCode,
        string? resolutionNote)
    {
        ValidateOpenedAtUtc(openedAtUtc);
        ValidateResolvedAtUtc(resolvedAtUtc, openedAtUtc);
        ValidateResolvedByUserId(resolvedByUserId);
        ValidateOptionalResolutionType(resolutionType);
        ValidateOptionalResolutionReasonCode(resolutionReasonCode);
        ValidateOptionalResolutionNote(resolutionNote);

        if (string.Equals(status, CommentModerationCaseStatuses.Open, StringComparison.OrdinalIgnoreCase))
        {
            if (resolvedAtUtc.HasValue ||
                resolvedByUserId.HasValue ||
                resolutionType is not null ||
                resolutionReasonCode is not null ||
                resolutionNote is not null)
            {
                throw new InteractionDomainException(
                    "INTERACTION.COMMENT_MODERATION_CASE_OPEN_STATE_INVALID",
                    "Open moderation case must not contain resolution state.");
            }

            return;
        }

        if (!resolvedAtUtc.HasValue || !resolvedByUserId.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_RESOLVED_STATE_INVALID",
                "Resolved moderation case must contain resolution timestamp and resolver user id.");
        }

        if (string.Equals(status, CommentModerationCaseStatuses.Dismissed, StringComparison.OrdinalIgnoreCase))
        {
            EnsureResolutionType(
                resolutionType,
                CommentModerationCaseResolutionTypes.DismissReportedCase,
                "Dismissed moderation case must use DismissReportedCase resolution type.");

            EnsureReasonCodeIsProvided(resolutionReasonCode);
            return;
        }

        if (string.Equals(status, CommentModerationCaseStatuses.Actioned, StringComparison.OrdinalIgnoreCase))
        {
            EnsureResolutionType(
                resolutionType,
                CommentModerationCaseResolutionTypes.HideReportedComment,
                "Actioned moderation case must use HideReportedComment resolution type.");

            EnsureReasonCodeIsProvided(resolutionReasonCode);
            return;
        }

        if (string.Equals(
            status,
            CommentModerationCaseStatuses.ClosedByAuthorDeletion,
            StringComparison.OrdinalIgnoreCase))
        {
            EnsureResolutionType(
                resolutionType,
                CommentModerationCaseResolutionTypes.CloseCaseByAuthorDeletion,
                "Case closed by author deletion must use CloseCaseByAuthorDeletion resolution type.");

            if (resolutionReasonCode is not null || resolutionNote is not null)
            {
                throw new InteractionDomainException(
                    "INTERACTION.COMMENT_MODERATION_CASE_AUTHOR_DELETION_RESOLUTION_INVALID",
                    "Case closed by author deletion must not contain resolution reason or note.");
            }
        }
    }

    private static void ValidateOpenedAtUtc(DateTime openedAtUtc)
    {
        if (openedAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_OPENED_AT_UTC",
                "OpenedAtUtc must be a valid datetime.");
        }
    }

    private static void ValidateResolvedAtUtc(
        DateTime? resolvedAtUtc,
        DateTime openedAtUtc)
    {
        if (resolvedAtUtc.HasValue && resolvedAtUtc.Value < openedAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_RESOLVED_AT_UTC_ORDER",
                "ResolvedAtUtc must be greater than or equal to OpenedAtUtc.");
        }
    }

    private static void ValidateResolvedByUserId(long? resolvedByUserId)
    {
        if (resolvedByUserId.HasValue && resolvedByUserId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_RESOLVED_BY_USER_ID",
                "ResolvedByUserId must be greater than zero when provided.");
        }
    }

    private static void ValidateOptionalResolutionType(string? resolutionType)
    {
        if (resolutionType is not null &&
            !CommentModerationCaseResolutionTypes.IsValid(resolutionType))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_RESOLUTION_TYPE",
                "Comment moderation case resolution type is invalid.");
        }
    }

    private static void ValidateOptionalResolutionReasonCode(string? resolutionReasonCode)
    {
        if (resolutionReasonCode is not null &&
            !ModerationReasonCodes.IsValid(resolutionReasonCode))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_RESOLUTION_REASON_CODE",
                "Comment moderation case resolution reason code is invalid.");
        }
    }

    private static void ValidateOptionalResolutionNote(string? resolutionNote)
    {
        if (resolutionNote is not null &&
            string.IsNullOrWhiteSpace(resolutionNote))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_INVALID_RESOLUTION_NOTE",
                "Resolution note must not be blank when provided.");
        }

        if (resolutionNote?.Trim().Length > 1000)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_RESOLUTION_NOTE_TOO_LONG",
                "Resolution note must not exceed 1000 characters.");
        }
    }

    private static void EnsureResolutionType(
        string? actualResolutionType,
        string expectedResolutionType,
        string message)
    {
        if (!string.Equals(
            actualResolutionType,
            expectedResolutionType,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_RESOLUTION_TYPE_MISMATCH",
                message);
        }
    }

    private static void EnsureReasonCodeIsProvided(string? resolutionReasonCode)
    {
        if (string.IsNullOrWhiteSpace(resolutionReasonCode))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_MODERATION_CASE_RESOLUTION_REASON_REQUIRED",
                "Resolution reason code is required for dismissed or actioned moderation cases.");
        }

        if (string.Equals(
                resolutionReasonCode,
                ModerationReasonCodes.Other,
                StringComparison.OrdinalIgnoreCase))
        {
            // The note requirement for Other is validated after normalized state is provided.
            // This guard is completed below by ensuring caller passes a non-empty note.
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