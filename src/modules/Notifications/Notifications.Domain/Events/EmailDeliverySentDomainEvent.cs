namespace Notifications.Domain.Events;

public sealed record EmailDeliverySentDomainEvent(
    long EmailDeliveryId,
    string MessageId,
    long? RecipientUserId,
    string ToEmail,
    string TemplateKey,
    string Provider,
    string? ProviderMessageId,
    string? CorrelationId,
    DateTime SentAtUtc);