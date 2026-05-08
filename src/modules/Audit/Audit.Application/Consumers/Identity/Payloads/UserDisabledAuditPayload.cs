namespace Audit.Application.Consumers.Identity.Payloads;

public sealed class UserDisabledAuditPayload
{
    public long TargetUserId { get; init; }

    public string TargetUserPublicId { get; init; } = string.Empty;

    public string TargetEmail { get; init; } = string.Empty;

    public string? TargetFullName { get; init; }

    public long ActorUserId { get; init; }

    public string? Reason { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public bool SessionsRevoked { get; init; }

    public int RevokedSessionCount { get; init; }

    public DateTime DisabledAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}