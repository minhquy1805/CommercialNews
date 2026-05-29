namespace Reading.Application.Consumers.Interaction;

public sealed class InteractionReadingEnvelopeContext
{
    public required string MessageId { get; init; }

    public required string EventType { get; init; }

    public required string AggregateType { get; init; }

    public required string AggregateId { get; init; }

    public string? AggregatePublicId { get; init; }

    public int? AggregateVersion { get; init; }

    public string? CorrelationId { get; init; }

    public long? InitiatorUserId { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public static InteractionReadingEnvelopeContext Create(
        string messageId,
        string eventType,
        string aggregateType,
        string aggregateId,
        string? aggregatePublicId,
        int? aggregateVersion,
        string? correlationId,
        long? initiatorUserId,
        DateTime occurredAtUtc)
    {
        return new InteractionReadingEnvelopeContext
        {
            MessageId = messageId,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            AggregatePublicId = aggregatePublicId,
            AggregateVersion = aggregateVersion,
            CorrelationId = correlationId,
            InitiatorUserId = initiatorUserId,
            OccurredAtUtc = occurredAtUtc
        };
    }
}
