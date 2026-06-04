namespace Audit.Infrastructure.Normalization.Identity.EventPayloads;

internal sealed class EmailVerifiedAuditPayload
{
    public long UserId { get; init; }

    public string UserPublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public long VerificationTokenId { get; init; }

    public DateTime VerifiedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

internal sealed class PasswordChangedAuditPayload
{
    public long UserId { get; init; }

    public string UserPublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTime ChangedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

internal sealed class EmailMarkedVerifiedAuditPayload
{
    public long TargetUserId { get; init; }

    public string TargetUserPublicId { get; init; } = string.Empty;

    public string TargetEmail { get; init; } = string.Empty;

    public string? TargetFullName { get; init; }

    public long ActorUserId { get; init; }

    public string? Reason { get; init; }

    public bool WasAlreadyVerified { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public DateTime MarkedVerifiedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

internal sealed class UserActivatedAuditPayload
{
    public long TargetUserId { get; init; }

    public string TargetUserPublicId { get; init; } = string.Empty;

    public string TargetEmail { get; init; } = string.Empty;

    public string? TargetFullName { get; init; }

    public long ActorUserId { get; init; }

    public string? Reason { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public DateTime ActivatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

internal sealed class UserDisabledAuditPayload
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

internal sealed class UserLockedAuditPayload
{
    public long TargetUserId { get; init; }

    public string TargetUserPublicId { get; init; } = string.Empty;

    public string TargetEmail { get; init; } = string.Empty;

    public string? TargetFullName { get; init; }

    public long ActorUserId { get; init; }

    public string? Reason { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public DateTime LockedUntilUtc { get; init; }

    public bool SessionsRevoked { get; init; }

    public int RevokedSessionCount { get; init; }

    public DateTime LockedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

internal sealed class UserSessionsRevokedAuditPayload
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

internal sealed class UserUnlockedAuditPayload
{
    public long TargetUserId { get; init; }

    public string TargetUserPublicId { get; init; } = string.Empty;

    public string TargetEmail { get; init; } = string.Empty;

    public string? TargetFullName { get; init; }

    public long ActorUserId { get; init; }

    public string? Reason { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public DateTime UnlockedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
