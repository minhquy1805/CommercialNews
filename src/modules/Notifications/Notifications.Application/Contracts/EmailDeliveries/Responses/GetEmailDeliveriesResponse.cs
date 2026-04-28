namespace Notifications.Application.Contracts.EmailDeliveries.Responses;

public sealed class GetEmailDeliveriesResponse
{
    public IReadOnlyList<EmailDeliveryListItemResponse> Items { get; init; }
        = Array.Empty<EmailDeliveryListItemResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }
}