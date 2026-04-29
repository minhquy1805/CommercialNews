namespace Notifications.Application.Outbox.Payloads;

public sealed record EmailSentIntegrationEventPayload(
    long EmailDeliveryId,
    long EmailDeliveryAttemptId,
    string MessageId,
    string BusinessDedupeKey,
    long? RecipientUserId,
    string ToEmail,
    string TemplateKey,
    string Provider,
    int AttemptCount,
    string? ProviderMessageId,
    string? CorrelationId,
    DateTime SentAtUtc);