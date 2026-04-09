namespace Notifications.Application.Contracts.EmailDeliveries.Requests;

public sealed class GetEmailDeliveryByIdRequest
{
    public long EmailDeliveryId { get; init; }
}