namespace Notifications.Application.Contracts.Ingestion;

public sealed class EmailNotificationIngestionRequest
{
    public required string MessageId { get; init; }

    public required string BusinessDedupeKey { get; init; }

    public long? RecipientUserId { get; init; }

    public required string ToEmail { get; init; }

    public required string TemplateKey { get; init; }

    public required string VariablesJson { get; init; }

    public required string Provider { get; init; }

    public byte Priority { get; init; } = 5;

    public string? CorrelationId { get; init; }

    public string? SourceModule { get; init; }

    public string? SourceEventType { get; init; }

    public DateTime? OccurredAtUtc { get; init; }
}