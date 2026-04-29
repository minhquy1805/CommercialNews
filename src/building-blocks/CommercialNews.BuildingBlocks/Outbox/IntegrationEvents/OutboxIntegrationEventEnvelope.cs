using System.Text.Json;

namespace CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;

public sealed record OutboxIntegrationEventEnvelope(
    string MessageId,
    string EventType,
    string AggregateType,
    string AggregateId,
    string? AggregatePublicId,
    int? AggregateVersion,
    JsonElement Payload,
    JsonElement? Headers,
    string? CorrelationId,
    long? InitiatorUserId,
    DateTime OccurredAtUtc);