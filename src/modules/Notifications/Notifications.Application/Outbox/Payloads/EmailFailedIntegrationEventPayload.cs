namespace Notifications.Application.Outbox.Payloads;

public sealed record EmailFailedIntegrationEventPayload(
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