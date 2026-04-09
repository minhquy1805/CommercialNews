namespace Notifications.Application.Contracts.Services;

public sealed class ProviderClassificationResult
{
    public bool IsSuccess { get; init; }

    public bool IsAmbiguous { get; init; }

    public bool IsRetryable { get; init; }

    public string? ErrorClass { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}