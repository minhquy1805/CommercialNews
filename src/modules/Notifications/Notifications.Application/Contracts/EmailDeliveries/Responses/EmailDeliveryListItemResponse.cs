namespace Notifications.Application.Contracts.EmailDeliveries.Responses;

public sealed class EmailDeliveryListItemResponse
{
    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public long? RecipientUserId { get; init; }

    public string? MaskedToEmail { get; init; }

    public string TemplateKey { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTime? LastAttemptAt { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public DateTime? SentAt { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }

    public string? CorrelationId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}