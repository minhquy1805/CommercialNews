namespace Notifications.Application.Contracts.Services;

public sealed class NotificationRenderResult
{
    public bool IsSuccess { get; init; }

    public string? Subject { get; init; }

    public string? Body { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}