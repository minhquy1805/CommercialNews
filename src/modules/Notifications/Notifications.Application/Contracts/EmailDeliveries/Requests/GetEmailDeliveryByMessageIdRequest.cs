namespace Notifications.Application.Contracts.EmailDeliveries.Requests;

public sealed class GetEmailDeliveryByMessageIdRequest
{
    public string MessageId { get; init; } = string.Empty;
}