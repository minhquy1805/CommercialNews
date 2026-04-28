namespace Notifications.Domain.Events;

public sealed record EmailDeliveryFailedDomainEvent(
    long EmailDeliveryId,
    string MessageId,
    long? RecipientUserId,
    string ToEmail,
    string TemplateKey,
    string Provider,
    int AttemptCount,
    DateTime? NextRetryAtUtc,
    string? LastErrorCode,
    string? LastErrorClass,
    string? CorrelationId,
    DateTime FailedAtUtc);