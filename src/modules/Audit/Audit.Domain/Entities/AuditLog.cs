namespace Audit.Domain.Entities;

using Audit.Domain.Enums;
using Audit.Domain.Exceptions;

public sealed class AuditLog
{
    public long AuditId { get; private set; }

    public Guid AuditEventId { get; private set; }

    public long? ActorUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public string ResourceId { get; private set; } = string.Empty;

    public string? Outcome { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public string? Reason { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? OldValuesJson { get; private set; }

    public string? NewValuesJson { get; private set; }

    public string? MetadataJson { get; private set; }

    private AuditLog()
    {
    }

    public static AuditLog Create(
        Guid auditEventId,
        long? actorUserId,
        string action,
        string resourceType,
        string resourceId,
        string? outcome,
        string summary,
        string? reason,
        DateTime occurredAt,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? metadataJson = null)
    {
        ValidateAuditEventId(auditEventId);
        ValidateActorUserId(actorUserId);
        ValidateAction(action);
        ValidateResourceType(resourceType);
        ValidateResourceId(resourceId);
        ValidateOutcome(outcome);
        ValidateSummary(summary);
        ValidateReason(reason);
        ValidateOccurredAt(occurredAt);
        ValidateCorrelationId(correlationId);
        ValidateIpAddress(ipAddress);
        ValidateUserAgent(userAgent);

        return new AuditLog
        {
            AuditEventId = auditEventId,
            ActorUserId = actorUserId,
            Action = action.Trim(),
            ResourceType = resourceType.Trim(),
            ResourceId = resourceId.Trim(),
            Outcome = NormalizeOptional(outcome),
            Summary = summary.Trim(),
            Reason = NormalizeOptional(reason),
            OccurredAt = occurredAt,
            CorrelationId = NormalizeOptional(correlationId),
            IpAddress = NormalizeOptional(ipAddress),
            UserAgent = NormalizeOptional(userAgent),
            OldValuesJson = NormalizeOptional(oldValuesJson),
            NewValuesJson = NormalizeOptional(newValuesJson),
            MetadataJson = NormalizeOptional(metadataJson)
        };
    }

    public static AuditLog Rehydrate(
        long auditId,
        Guid auditEventId,
        long? actorUserId,
        string action,
        string resourceType,
        string resourceId,
        string? outcome,
        string summary,
        string? reason,
        DateTime occurredAt,
        string? correlationId,
        string? ipAddress,
        string? userAgent,
        string? oldValuesJson,
        string? newValuesJson,
        string? metadataJson)
    {
        if (auditId <= 0)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_ID",
                "Audit id must be greater than zero.");
        }

        ValidateAuditEventId(auditEventId);
        ValidateActorUserId(actorUserId);
        ValidateAction(action);
        ValidateResourceType(resourceType);
        ValidateResourceId(resourceId);
        ValidateOutcome(outcome);
        ValidateSummary(summary);
        ValidateReason(reason);
        ValidateOccurredAt(occurredAt);
        ValidateCorrelationId(correlationId);
        ValidateIpAddress(ipAddress);
        ValidateUserAgent(userAgent);

        return new AuditLog
        {
            AuditId = auditId,
            AuditEventId = auditEventId,
            ActorUserId = actorUserId,
            Action = action.Trim(),
            ResourceType = resourceType.Trim(),
            ResourceId = resourceId.Trim(),
            Outcome = NormalizeOptional(outcome),
            Summary = summary.Trim(),
            Reason = NormalizeOptional(reason),
            OccurredAt = occurredAt,
            CorrelationId = NormalizeOptional(correlationId),
            IpAddress = NormalizeOptional(ipAddress),
            UserAgent = NormalizeOptional(userAgent),
            OldValuesJson = NormalizeOptional(oldValuesJson),
            NewValuesJson = NormalizeOptional(newValuesJson),
            MetadataJson = NormalizeOptional(metadataJson)
        };
    }

    private static void ValidateAuditEventId(Guid auditEventId)
    {
        if (auditEventId == Guid.Empty)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_EVENT_ID",
                "Audit event id must not be empty.");
        }
    }

    private static void ValidateActorUserId(long? actorUserId)
    {
        if (actorUserId.HasValue && actorUserId.Value <= 0)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_ACTOR_USER_ID",
                "Actor user id must be greater than zero when provided.");
        }
    }

    private static void ValidateAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_ACTION",
                "Audit action is required.");
        }

        if (action.Trim().Length > 120)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_ACTION_TOO_LONG",
                "Audit action must not exceed 120 characters.");
        }
    }

    private static void ValidateResourceType(string resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_RESOURCE_TYPE",
                "Resource type is required.");
        }

        if (resourceType.Trim().Length > 60)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_RESOURCE_TYPE_TOO_LONG",
                "Resource type must not exceed 60 characters.");
        }
    }

    private static void ValidateResourceId(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_RESOURCE_ID",
                "Resource id is required.");
        }

        if (resourceId.Trim().Length > 100)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_RESOURCE_ID_TOO_LONG",
                "Resource id must not exceed 100 characters.");
        }
    }

    private static void ValidateOutcome(string? outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            return;
        }

        if (!AuditOutcome.IsValid(outcome))
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_OUTCOME",
                "Audit outcome is invalid.");
        }
    }

    private static void ValidateSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_SUMMARY",
                "Audit summary is required.");
        }

        if (summary.Trim().Length > 300)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_SUMMARY_TOO_LONG",
                "Audit summary must not exceed 300 characters.");
        }
    }

    private static void ValidateReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        if (reason.Trim().Length > 500)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_REASON_TOO_LONG",
                "Audit reason must not exceed 500 characters.");
        }
    }

    private static void ValidateOccurredAt(DateTime occurredAt)
    {
        if (occurredAt == default)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_INVALID_OCCURRED_AT",
                "OccurredAt must be a valid UTC datetime.");
        }
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        if (correlationId.Trim().Length > 100)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_CORRELATION_ID_TOO_LONG",
                "CorrelationId must not exceed 100 characters.");
        }
    }

    private static void ValidateIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return;
        }

        if (ipAddress.Trim().Length > 45)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_IP_ADDRESS_TOO_LONG",
                "IpAddress must not exceed 45 characters.");
        }
    }

    private static void ValidateUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return;
        }

        if (userAgent.Trim().Length > 300)
        {
            throw new AuditDomainException(
                "AUDIT.AUDIT_LOG_USER_AGENT_TOO_LONG",
                "UserAgent must not exceed 300 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}