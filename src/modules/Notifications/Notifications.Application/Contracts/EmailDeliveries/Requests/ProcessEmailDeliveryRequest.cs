namespace Notifications.Application.Contracts.EmailDeliveries.Requests;

public sealed class ProcessEmailDeliveryRequest
{
    public long EmailDeliveryId { get; init; }

    public string? CorrelationId { get; init; }
}