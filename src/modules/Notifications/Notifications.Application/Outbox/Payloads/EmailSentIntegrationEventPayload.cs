namespace Notifications.Application.Outbox.Payloads;

public sealed record EmailSentIntegrationEventPayload(
    long EmailDeliveryId,
    string MessageId,
    long? RecipientUserId,
    string ToEmail,
    string TemplateKey,
    string Provider,
    string? ProviderMessageId,
    string? CorrelationId,
    DateTime SentAtUtc);