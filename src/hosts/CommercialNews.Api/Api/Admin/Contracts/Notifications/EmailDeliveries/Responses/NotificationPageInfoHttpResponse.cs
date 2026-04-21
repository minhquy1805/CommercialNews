namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;

public sealed class NotificationPageInfoHttpResponse
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}