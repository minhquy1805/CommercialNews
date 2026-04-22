namespace Notifications.Application.Contracts.EmailDeliveries.Requests;

public sealed class ProcessPendingEmailDeliveriesRequest
{
    public int TopN { get; init; } = 20;
}