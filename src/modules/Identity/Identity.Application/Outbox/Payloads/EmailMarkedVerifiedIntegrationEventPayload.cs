namespace Identity.Application.Outbox.Payloads;

public sealed record EmailMarkedVerifiedIntegrationEventPayload(
    long TargetUserId,
    string TargetUserPublicId,
    string TargetEmail,
    string? TargetFullName,
    long ActorUserId,
    string? Reason,
    bool WasAlreadyVerified,
    string PreviousStatus,
    string NewStatus,
    DateTime MarkedVerifiedAtUtc,
    string BusinessDedupeKey);