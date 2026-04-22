namespace Notifications.Application.Models.OutboxPayloads;

public sealed class IdentityVerificationEmailRequestedPayload
{
    public string BusinessDedupeKey { get; init; } = string.Empty;

    public long RecipientUserId { get; init; }

    public string ToEmail { get; init; } = string.Empty;

    public string TemplateKey { get; init; } = string.Empty;

    public int? TemplateVersion { get; init; }

    public string? Subject { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public IdentityTemplateVariables Variables { get; init; } = new();
}

