namespace Audit.Application.Consumers.Identity.Payloads;

public sealed class EmailVerifiedAuditPayload
{
    public long UserId { get; init; }

    public string UserPublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public long VerificationTokenId { get; init; }

    public DateTime VerifiedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}