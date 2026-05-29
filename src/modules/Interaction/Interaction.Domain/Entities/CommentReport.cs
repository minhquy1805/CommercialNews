using Interaction.Domain.Constants;
using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class CommentReport
{
    public long CommentReportId { get; private set; }

    public string PublicId { get; private set; } = string.Empty;

    public long CommentId { get; private set; }
    public long CommentModerationCaseId { get; private set; }

    public long ReporterUserId { get; private set; }

    public string ReasonCode { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public string Status { get; private set; } = CommentReportStatuses.Pending;

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    private CommentReport()
    {
    }

    /// <summary>
    /// Rehydrates a persisted comment report.
    /// Report creation and resolution are performed through workflow procedures
    /// because they also coordinate moderation-case and alert state.
    /// </summary>
    public static CommentReport Rehydrate(
        long commentReportId,
        string publicId,
        long commentId,
        long commentModerationCaseId,
        long reporterUserId,
        string reasonCode,
        string? description,
        string status,
        DateTime createdAtUtc,
        DateTime? resolvedAtUtc)
    {
        ValidateCommentReportId(commentReportId);
        ValidatePublicId(publicId);
        ValidateCommentId(commentId);
        ValidateCommentModerationCaseId(commentModerationCaseId);
        ValidateReporterUserId(reporterUserId);
        ValidateReasonCode(reasonCode);
        ValidateDescription(reasonCode, description);
        ValidateStatus(status);
        ValidateState(status, createdAtUtc, resolvedAtUtc);

        return new CommentReport
        {
            CommentReportId = commentReportId,
            PublicId = NormalizeRequired(publicId),
            CommentId = commentId,
            CommentModerationCaseId = commentModerationCaseId,
            ReporterUserId = reporterUserId,
            ReasonCode = NormalizeRequired(reasonCode),
            Description = NormalizeOptional(description),
            Status = NormalizeRequired(status),
            CreatedAtUtc = createdAtUtc,
            ResolvedAtUtc = resolvedAtUtc
        };
    }

    public bool IsPending()
    {
        return string.Equals(
            Status,
            CommentReportStatuses.Pending,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDismissed()
    {
        return string.Equals(
            Status,
            CommentReportStatuses.Dismissed,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsActioned()
    {
        return string.Equals(
            Status,
            CommentReportStatuses.Actioned,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsClosedByAuthorDeletion()
    {
        return string.Equals(
            Status,
            CommentReportStatuses.ClosedByAuthorDeletion,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCommentReportId(long commentReportId)
    {
        if (commentReportId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_ID",
                "Comment report id must be greater than zero.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_PUBLIC_ID_REQUIRED",
                "Comment report public id is required.");
        }
    }

    private static void ValidateCommentId(long commentId)
    {
        if (commentId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_COMMENT_ID",
                "Comment id must be greater than zero.");
        }
    }

    private static void ValidateCommentModerationCaseId(long commentModerationCaseId)
    {
        if (commentModerationCaseId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_MODERATION_CASE_ID",
                "Comment moderation case id must be greater than zero.");
        }
    }

    private static void ValidateReporterUserId(long reporterUserId)
    {
        if (reporterUserId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_REPORTER_USER_ID",
                "Reporter user id must be greater than zero.");
        }
    }

    private static void ValidateReasonCode(string reasonCode)
    {
        if (!CommentReportReasonCodes.IsValid(reasonCode))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_REASON_CODE",
                "Comment report reason code is invalid.");
        }
    }

    private static void ValidateDescription(
        string reasonCode,
        string? description)
    {
        if (description is not null && string.IsNullOrWhiteSpace(description))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_DESCRIPTION",
                "Comment report description must not be blank when provided.");
        }

        if (description?.Trim().Length > 1000)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_DESCRIPTION_TOO_LONG",
                "Comment report description must not exceed 1000 characters.");
        }

        if (CommentReportReasonCodes.RequiresDescription(reasonCode) &&
            string.IsNullOrWhiteSpace(description))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_DESCRIPTION_REQUIRED",
                "Comment report description is required when reason code is Other.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (!CommentReportStatuses.IsValid(status))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_STATUS",
                "Comment report status is invalid.");
        }
    }

    private static void ValidateState(
        string status,
        DateTime createdAtUtc,
        DateTime? resolvedAtUtc)
    {
        if (createdAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_CREATED_AT_UTC",
                "CreatedAtUtc must be a valid datetime.");
        }

        if (resolvedAtUtc.HasValue && resolvedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_INVALID_RESOLVED_AT_UTC_ORDER",
                "ResolvedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        var isPending = string.Equals(
            status,
            CommentReportStatuses.Pending,
            StringComparison.OrdinalIgnoreCase);

        if (isPending && resolvedAtUtc.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_PENDING_STATE_INVALID",
                "Pending comment report must not have ResolvedAtUtc.");
        }

        if (!isPending && !resolvedAtUtc.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPORT_RESOLVED_STATE_INVALID",
                "Resolved comment report must have ResolvedAtUtc.");
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