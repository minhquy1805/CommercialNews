namespace Notifications.Application.Contracts.EmailDeliveries.Responses;

public sealed class RetryEmailDeliveryResponse
{
    public bool Accepted { get; init; }

    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}