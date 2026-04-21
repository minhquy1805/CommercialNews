namespace Notifications.Application.Contracts.EmailDeliveries.Responses;

public sealed class GetEmailDeliveryAttemptsResponse
{
    public long EmailDeliveryId { get; init; }

    public IReadOnlyList<EmailDeliveryAttemptItemResponse> Items { get; init; }
        = Array.Empty<EmailDeliveryAttemptItemResponse>();
}