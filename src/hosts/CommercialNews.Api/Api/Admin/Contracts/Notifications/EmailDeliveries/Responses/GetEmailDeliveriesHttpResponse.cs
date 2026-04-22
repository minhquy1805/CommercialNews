namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;

public sealed class GetEmailDeliveriesHttpResponse
{
    public IReadOnlyList<EmailDeliveryListItemHttpResponse> Items { get; init; }
        = Array.Empty<EmailDeliveryListItemHttpResponse>();

    public NotificationPageInfoHttpResponse PageInfo { get; init; } = new();
}
