namespace Notifications.Application.Contracts.Processing.Requests;

public sealed class ProcessEmailDeliveryRequest
{
    public long EmailDeliveryId { get; init; }

    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string? CorrelationId { get; init; }
}