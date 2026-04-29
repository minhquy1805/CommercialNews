namespace Notifications.Application.Consumers.Identity.Payloads;

public sealed record IdentityVerificationEmailRequestedPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long VerificationTokenId,
    string RawVerificationToken,
    DateTime ExpiresAtUtc,
    string BusinessDedupeKey);