namespace Notifications.Application.Outbox.Payloads;

public sealed record EmailFailedIntegrationEventPayload(
    long EmailDeliveryId,
    long EmailDeliveryAttemptId,
    string MessageId,
    string BusinessDedupeKey,
    long? RecipientUserId,
    string ToEmail,
    string TemplateKey,
    string Provider,
    int AttemptCount,
    DateTime? NextRetryAtUtc,
    string? LastErrorCode,
    string? LastErrorClass,
    bool IsAmbiguous,
    string? CorrelationId,
    DateTime FailedAtUtc);