namespace Audit.Application.Consumers.Identity.Payloads;

public sealed class EmailMarkedVerifiedAuditPayload
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