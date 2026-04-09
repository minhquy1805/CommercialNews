namespace Notifications.Application.Contracts.EmailDeliveries.Requests;

public sealed class RetryEmailDeliveryRequest
{
    public long EmailDeliveryId { get; init; }

    public long? ActorUserId { get; init; }

    public string? CorrelationId { get; init; }
}