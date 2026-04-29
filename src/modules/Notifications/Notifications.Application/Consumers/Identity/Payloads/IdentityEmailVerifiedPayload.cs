namespace Notifications.Application.Consumers.Identity.Payloads;

public sealed record IdentityEmailVerifiedPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long VerificationTokenId,
    DateTime VerifiedAtUtc,
    string BusinessDedupeKey);