namespace Notifications.Application.Contracts.Services;

public sealed class EmailSendRequest
{
    public string ToEmail { get; init; } = string.Empty;

    public string TemplateKey { get; init; } = string.Empty;

    public string? Subject { get; init; }

    public string Body { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }
}