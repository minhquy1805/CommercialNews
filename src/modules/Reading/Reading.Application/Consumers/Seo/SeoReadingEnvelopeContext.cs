namespace Reading.Application.Consumers.Seo;

public sealed class SeoReadingEnvelopeContext
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

    public static SeoReadingEnvelopeContext Create(
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
        return new SeoReadingEnvelopeContext
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
