namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;

public sealed class GetEmailDeliveryAttemptsHttpResponse
{
    public long EmailDeliveryId { get; init; }

    public IReadOnlyList<EmailDeliveryAttemptHttpResponse> Items { get; init; }
        = Array.Empty<EmailDeliveryAttemptHttpResponse>();
}