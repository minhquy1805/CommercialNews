namespace Notifications.Application.Contracts.EmailDeliveries.Responses;

public sealed class EmailDeliveryAttemptItemResponse
{
    public long EmailDeliveryAttemptId { get; init; }

    public long EmailDeliveryId { get; init; }

    public int AttemptNumber { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public bool IsAmbiguous { get; init; }

    public string? ProviderMessageId { get; init; }

    public string? ProviderErrorCode { get; init; }

    public string? ErrorClass { get; init; }

    public string? ErrorDetail { get; init; }

    public string? CorrelationId { get; init; }

    public DateTime CreatedAt { get; init; }
}