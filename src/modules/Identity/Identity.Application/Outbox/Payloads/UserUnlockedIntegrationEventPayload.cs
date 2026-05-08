namespace Identity.Application.Outbox.Payloads;

public sealed record UserUnlockedIntegrationEventPayload(
    long TargetUserId,
    string TargetUserPublicId,
    string TargetEmail,
    string? TargetFullName,
    long ActorUserId,
    string? Reason,
    string PreviousStatus,
    string NewStatus,
    DateTime UnlockedAtUtc,
    string BusinessDedupeKey);