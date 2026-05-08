namespace Audit.Application.Consumers.Identity.Payloads;

public sealed class PasswordChangedAuditPayload
{
    public long UserId { get; init; }

    public string UserPublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTime ChangedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}