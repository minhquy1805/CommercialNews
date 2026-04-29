namespace Notifications.Domain.Events;

public sealed record EmailDeliverySentDomainEvent(
    long EmailDeliveryId,
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