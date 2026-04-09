namespace Notifications.Application.Contracts.Services;

public sealed class EmailSendResult
{
    public bool IsSuccess { get; init; }

    public bool IsAmbiguous { get; init; }

    public string? ProviderMessageId { get; init; }

    public string? ProviderErrorCode { get; init; }

    public string? ProviderErrorMessage { get; init; }
}