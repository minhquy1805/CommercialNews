using CommercialNews.BuildingBlocks.Outbox.Enums;

namespace CommercialNews.BuildingBlocks.Outbox.Models;

public sealed class OutboxMessage
{
    public long OutboxMessageId { get; private set; }

    public string MessageId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public string AggregateType { get; private set; } = string.Empty;

    public string AggregateId { get; private set; } = string.Empty;

    public string? AggregatePublicId { get; private set; }

    public int? AggregateVersion { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public string? Headers { get; private set; }

    public string? CorrelationId { get; private set; }

    public long? InitiatorUserId { get; private set; }

    public byte Priority { get; private set; }

    public string Status { get; private set; } = OutboxMessageStatus.Pending;

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
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("MessageId is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("EventType is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(aggregateType))
        {
            throw new ArgumentException("AggregateType is required.", nameof(aggregateType));
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("AggregateId is required.", nameof(aggregateId));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload is required.", nameof(payload));
        }

        if (priority is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 1 and 10.");
        }

        return new OutboxMessage
        {
            MessageId = messageId.Trim(),
            EventType = eventType.Trim(),
            AggregateType = aggregateType.Trim(),
            AggregateId = aggregateId.Trim(),
            AggregatePublicId = string.IsNullOrWhiteSpace(aggregatePublicId) ? null : aggregatePublicId.Trim(),
            AggregateVersion = aggregateVersion,
            Payload = payload,
            Headers = string.IsNullOrWhiteSpace(headers) ? null : headers,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim(),
            InitiatorUserId = initiatorUserId,
            Priority = priority,
            Status = OutboxMessageStatus.Pending,
            AttemptCount = 0,
            OccurredAt = occurredAt,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt
        };
    }

    public static OutboxMessage Rehydrate(
        long outboxMessageId,
        string messageId,
        string eventType,
        string aggregateType,
        string aggregateId,
        string payload,
        byte priority,
        string status,
        int attemptCount,
        DateTime occurredAt,
        DateTime createdAt,
        DateTime updatedAt,
        string? aggregatePublicId = null,
        int? aggregateVersion = null,
        string? headers = null,
        string? correlationId = null,
        long? initiatorUserId = null,
        DateTime? nextRetryAt = null,
        DateTime? lastAttemptAt = null,
        DateTime? publishedAt = null,
        string? lastError = null,
        string? lastErrorCode = null,
        string? lastErrorClass = null)
    {
        return new OutboxMessage
        {
            OutboxMessageId = outboxMessageId,
            MessageId = messageId,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            AggregatePublicId = aggregatePublicId,
            AggregateVersion = aggregateVersion,
            Payload = payload,
            Headers = headers,
            CorrelationId = correlationId,
            InitiatorUserId = initiatorUserId,
            Priority = priority,
            Status = status,
            AttemptCount = attemptCount,
            NextRetryAt = nextRetryAt,
            LastAttemptAt = lastAttemptAt,
            PublishedAt = publishedAt,
            LastError = lastError,
            LastErrorCode = lastErrorCode,
            LastErrorClass = lastErrorClass,
            OccurredAt = occurredAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}