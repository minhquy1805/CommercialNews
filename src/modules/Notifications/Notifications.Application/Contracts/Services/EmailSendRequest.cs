namespace Notifications.Application.Contracts.Services;

public sealed class EmailSendRequest
{
    public string MessageId { get; init; } = string.Empty;

    public string ToEmail { get; init; } = string.Empty;

    public string TemplateKey { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }
}