using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Domain.Entities;

public sealed class OutboxMessage
{
    public long OutboxMessageId { get; private set; }

    public string MessageId { get; private set; }

    public string EventType { get; private set; }

    public string AggregateType { get; private set; }

    public string AggregateId { get; private set; }

    public string? AggregatePublicId { get; private set; }

    public int? AggregateVersion { get; private set; }

    public string Payload { get; private set; }

    public string? Headers { get; private set; }

    public string? CorrelationId { get; private set; }

    public long? InitiatorUserId { get; private set; }

    public byte Priority { get; private set; }

    public string Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    public DateTime? LastAttemptAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public string? LastError { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorClass { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(
        string messageId,
        string eventType,
        string aggregateType,
        string aggregateId,
        string payload,
        DateTime occurredAt,
        byte priority = 5,
        string? aggregatePublicId = null,
        int? aggregateVersion = null,
        string? headers = null,
        string? correlationId = null,
        long? initiatorUserId = null)
    {
        ValidateMessageId(messageId);
        ValidateEventType(eventType);
        ValidateAggregateType(aggregateType);
        ValidateAggregateId(aggregateId);
        ValidateAggregatePublicId(aggregatePublicId);
        ValidateAggregateVersion(aggregateVersion);
        ValidatePayload(payload);
        ValidateHeaders(headers);
        ValidateCorrelationId(correlationId);
        ValidatePriority(priority);
        ValidateOccurredAt(occurredAt);

        DateTime nowUtc = DateTime.UtcNow;

        return new OutboxMessage
        {
            MessageId = NormalizeRequired(messageId),
            EventType = NormalizeRequired(eventType),
            AggregateType = NormalizeRequired(aggregateType),
            AggregateId = NormalizeRequired(aggregateId),
            AggregatePublicId = NormalizeOptional(aggregatePublicId),
            AggregateVersion = aggregateVersion,
            Payload = payload,
            Headers = NormalizeOptional(headers),
            CorrelationId = NormalizeOptional(correlationId),
            InitiatorUserId = initiatorUserId,
            Priority = priority,
            Status = OutboxMessageStatus.Pending,
            AttemptCount = 0,
            OccurredAt = occurredAt,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public static OutboxMessage Rehydrate(
        long outboxMessageId,
        string messageId,
        string eventType,
        string aggregateType,
        string aggregateId,
        string? aggregatePublicId,
        int? aggregateVersion,
        string payload,
        string? headers,
        string? correlationId,
        long? initiatorUserId,
        byte priority,
        string status,
        int attemptCount,
        DateTime? nextRetryAt,
        DateTime? lastAttemptAt,
        DateTime? publishedAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        DateTime occurredAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (outboxMessageId <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_INVALID_ID",
                "Outbox message id must be greater than zero.");
        }

        ValidateMessageId(messageId);
        ValidateEventType(eventType);
        ValidateAggregateType(aggregateType);
        ValidateAggregateId(aggregateId);
        ValidateAggregatePublicId(aggregatePublicId);
        ValidateAggregateVersion(aggregateVersion);
        ValidatePayload(payload);
        ValidateHeaders(headers);
        ValidateCorrelationId(correlationId);
        ValidatePriority(priority);
        ValidateStatus(status);
        ValidateAttemptCount(attemptCount);
        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);
        ValidateOccurredAt(occurredAt);

        return new OutboxMessage
        {
            OutboxMessageId = outboxMessageId,
            MessageId = NormalizeRequired(messageId),
            EventType = NormalizeRequired(eventType),
            AggregateType = NormalizeRequired(aggregateType),
            AggregateId = NormalizeRequired(aggregateId),
            AggregatePublicId = NormalizeOptional(aggregatePublicId),
            AggregateVersion = aggregateVersion,
            Payload = payload,
            Headers = NormalizeOptional(headers),
            CorrelationId = NormalizeOptional(correlationId),
            InitiatorUserId = initiatorUserId,
            Priority = priority,
            Status = NormalizeRequired(status),
            AttemptCount = attemptCount,
            NextRetryAt = nextRetryAt,
            LastAttemptAt = lastAttemptAt,
            PublishedAt = publishedAt,
            LastError = NormalizeOptional(lastError),
            LastErrorCode = NormalizeOptional(lastErrorCode),
            LastErrorClass = NormalizeOptional(lastErrorClass),
            OccurredAt = occurredAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void MarkProcessing(DateTime nowUtc)
    {
        EnsureCanTransitionFrom(
            OutboxMessageStatus.Pending,
            OutboxMessageStatus.Failed);

        Status = OutboxMessageStatus.Processing;
        LastAttemptAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public void MarkPublished(DateTime nowUtc)
    {
        EnsureCanTransitionFrom(
            OutboxMessageStatus.Pending,
            OutboxMessageStatus.Processing,
            OutboxMessageStatus.Failed);

        Status = OutboxMessageStatus.Published;
        PublishedAt = nowUtc;
        NextRetryAt = null;
        LastError = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    public void MarkFailed(
        DateTime nowUtc,
        DateTime? nextRetryAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass)
    {
        EnsureCanTransitionFrom(
            OutboxMessageStatus.Pending,
            OutboxMessageStatus.Processing,
            OutboxMessageStatus.Failed);

        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);

        Status = OutboxMessageStatus.Failed;
        AttemptCount++;
        LastAttemptAt = nowUtc;
        NextRetryAt = nextRetryAt;
        LastError = NormalizeOptional(lastError);
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void MarkDeadLetter(
        DateTime nowUtc,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass)
    {
        EnsureCanTransitionFrom(
            OutboxMessageStatus.Pending,
            OutboxMessageStatus.Processing,
            OutboxMessageStatus.Failed);

        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);

        Status = OutboxMessageStatus.DeadLetter;
        AttemptCount++;
        LastAttemptAt = nowUtc;
        NextRetryAt = null;
        LastError = NormalizeOptional(lastError);
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void ResetToPending(DateTime nowUtc)
    {
        EnsureCanTransitionFrom(
            OutboxMessageStatus.Failed,
            OutboxMessageStatus.DeadLetter,
            OutboxMessageStatus.Processing);

        Status = OutboxMessageStatus.Pending;
        NextRetryAt = null;
        LastError = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    private void EnsureCanTransitionFrom(params string[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_INVALID_STATE_TRANSITION",
                $"Cannot transition outbox message from status '{Status}'.");
        }
    }

    private static void ValidateMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_MESSAGE_ID_REQUIRED",
                "Message id is required.");
        }

        if (messageId.Trim().Length > 26)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_MESSAGE_ID_TOO_LONG",
                "Message id must not exceed 26 characters.");
        }
    }

    private static void ValidateEventType(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_EVENT_TYPE_REQUIRED",
                "Event type is required.");
        }

        if (eventType.Trim().Length > 200)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_EVENT_TYPE_TOO_LONG",
                "Event type must not exceed 200 characters.");
        }
    }

    private static void ValidateAggregateType(string? aggregateType)
    {
        if (string.IsNullOrWhiteSpace(aggregateType))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_AGGREGATE_TYPE_REQUIRED",
                "Aggregate type is required.");
        }

        if (aggregateType.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_AGGREGATE_TYPE_TOO_LONG",
                "Aggregate type must not exceed 100 characters.");
        }
    }

    private static void ValidateAggregateId(string? aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_AGGREGATE_ID_REQUIRED",
                "Aggregate id is required.");
        }

        if (aggregateId.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_AGGREGATE_ID_TOO_LONG",
                "Aggregate id must not exceed 100 characters.");
        }
    }

    private static void ValidateAggregatePublicId(string? aggregatePublicId)
    {
        if (!string.IsNullOrWhiteSpace(aggregatePublicId) && aggregatePublicId.Trim().Length > 26)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_AGGREGATE_PUBLIC_ID_TOO_LONG",
                "Aggregate public id must not exceed 26 characters.");
        }
    }

    private static void ValidateAggregateVersion(int? aggregateVersion)
    {
        if (aggregateVersion is not null && aggregateVersion <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_AGGREGATE_VERSION_INVALID",
                "Aggregate version must be greater than zero.");
        }
    }

    private static void ValidatePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_PAYLOAD_REQUIRED",
                "Payload is required.");
        }
    }

    private static void ValidateHeaders(string? headers)
    {
        if (!string.IsNullOrWhiteSpace(headers) && headers.Trim().Length > 4000)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_HEADERS_TOO_LONG",
                "Headers must not exceed 4000 characters.");
        }
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId) && correlationId.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_CORRELATION_ID_TOO_LONG",
                "Correlation id must not exceed 100 characters.");
        }
    }

    private static void ValidatePriority(byte priority)
    {
        if (priority is < 1 or > 9)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_PRIORITY_INVALID",
                "Priority must be between 1 and 9.");
        }
    }

    private static void ValidateStatus(string? status)
    {
        if (!OutboxMessageStatus.IsValid(status))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_STATUS_INVALID",
                "Outbox message status is invalid.");
        }
    }

    private static void ValidateAttemptCount(int attemptCount)
    {
        if (attemptCount < 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_ATTEMPT_COUNT_INVALID",
                "Attempt count must not be negative.");
        }
    }

    private static void ValidateErrorCode(string? errorCode)
    {
        if (!string.IsNullOrWhiteSpace(errorCode) && errorCode.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_ERROR_CODE_TOO_LONG",
                "Error code must not exceed 100 characters.");
        }
    }

    private static void ValidateErrorClass(string? errorClass)
    {
        if (!string.IsNullOrWhiteSpace(errorClass) && !EmailErrorClass.IsValid(errorClass))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_ERROR_CLASS_INVALID",
                "Error class is invalid.");
        }
    }

    private static void ValidateOccurredAt(DateTime occurredAt)
    {
        if (occurredAt == default)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.OUTBOX_OCCURRED_AT_REQUIRED",
                "OccurredAt is required.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}