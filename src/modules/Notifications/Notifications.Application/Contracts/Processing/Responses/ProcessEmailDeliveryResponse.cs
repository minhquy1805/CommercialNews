namespace Notifications.Application.Contracts.Processing.Responses;

public sealed class ProcessEmailDeliveryResponse
{
    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public int AttemptNumber { get; init; }

    public string Status { get; init; } = string.Empty;

    public bool IsSuccess { get; init; }

    public bool IsAmbiguous { get; init; }

    public string? ProviderMessageId { get; init; }
}