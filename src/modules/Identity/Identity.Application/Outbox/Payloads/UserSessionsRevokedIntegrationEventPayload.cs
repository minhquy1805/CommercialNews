namespace Identity.Application.Outbox.Payloads;

public sealed record UserSessionsRevokedIntegrationEventPayload(
    long TargetUserId,
    string TargetUserPublicId,
    string TargetEmail,
    string? TargetFullName,
    long ActorUserId,
    string? Reason,
    int RevokedSessionCount,
    DateTime RevokedAtUtc,
    string BusinessDedupeKey);