namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;

public sealed class RetryEmailDeliveryHttpResponse
{
    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool Accepted { get; init; }
}