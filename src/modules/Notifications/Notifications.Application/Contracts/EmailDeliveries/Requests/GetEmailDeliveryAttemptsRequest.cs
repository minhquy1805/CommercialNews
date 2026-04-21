namespace Notifications.Application.Contracts.EmailDeliveries.Requests;

public sealed class GetEmailDeliveryAttemptsRequest
{
    public long EmailDeliveryId { get; init; }
}