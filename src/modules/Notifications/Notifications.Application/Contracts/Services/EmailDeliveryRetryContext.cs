namespace Notifications.Application.Contracts.Services;

public sealed class EmailDeliveryRetryContext
{
    public string TemplateKey { get; init; } = string.Empty;

    public string CurrentStatus { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public string? ErrorClass { get; init; }

    public string? ErrorCode { get; init; }

    public bool IsAmbiguous { get; init; }

    public DateTime NowUtc { get; init; }
}