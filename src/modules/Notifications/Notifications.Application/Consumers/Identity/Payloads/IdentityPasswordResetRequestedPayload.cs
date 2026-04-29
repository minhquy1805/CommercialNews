namespace Notifications.Application.Consumers.Identity.Payloads;

public sealed record IdentityPasswordResetRequestedPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long ResetTokenId,
    string RawResetToken,
    DateTime ExpiresAtUtc,
    string BusinessDedupeKey);