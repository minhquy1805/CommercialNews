namespace Notifications.Domain.Events;

public sealed record EmailDeliveryDeadDomainEvent(
    long EmailDeliveryId,
    string MessageId,
    long? RecipientUserId,
    string ToEmail,
    string TemplateKey,
    string Provider,
    int AttemptCount,
    string? LastErrorCode,
    string? LastErrorClass,
    string? CorrelationId,
    DateTime DeadAtUtc);