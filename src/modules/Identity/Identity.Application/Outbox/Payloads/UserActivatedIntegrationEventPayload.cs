namespace Identity.Application.Outbox.Payloads;

public sealed record UserActivatedIntegrationEventPayload(
    long TargetUserId,
    string TargetUserPublicId,
    string TargetEmail,
    string? TargetFullName,
    long ActorUserId,
    string? Reason,
    string PreviousStatus,
    string NewStatus,
    DateTime ActivatedAtUtc,
    string BusinessDedupeKey);