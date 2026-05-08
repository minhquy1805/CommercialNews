namespace Identity.Application.Outbox.Payloads;

public sealed record UserLockedIntegrationEventPayload(
    long TargetUserId,
    string TargetUserPublicId,
    string TargetEmail,
    string? TargetFullName,
    long ActorUserId,
    string? Reason,
    string PreviousStatus,
    string NewStatus,
    DateTime LockedUntilUtc,
    bool SessionsRevoked,
    int RevokedSessionCount,
    DateTime LockedAtUtc,
    string BusinessDedupeKey);