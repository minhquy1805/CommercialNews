namespace Audit.Application.Consumers.Identity.Payloads;

public sealed class UserSessionsRevokedAuditPayload
{
    public long TargetUserId { get; init; }

    public string TargetUserPublicId { get; init; } = string.Empty;

    public string TargetEmail { get; init; } = string.Empty;

    public string? TargetFullName { get; init; }

    public long ActorUserId { get; init; }

    public string? Reason { get; init; }

    public int RevokedSessionCount { get; init; }

    public DateTime RevokedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}