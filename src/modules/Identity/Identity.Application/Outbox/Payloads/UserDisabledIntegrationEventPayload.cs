namespace Identity.Application.Outbox.Payloads;

public sealed record UserDisabledIntegrationEventPayload(
    long TargetUserId,
    string TargetUserPublicId,
    string TargetEmail,
    string? TargetFullName,
    long ActorUserId,
    string? Reason,
    string PreviousStatus,
    string NewStatus,
    bool SessionsRevoked,
    int RevokedSessionCount,
    DateTime DisabledAtUtc,
    string BusinessDedupeKey);