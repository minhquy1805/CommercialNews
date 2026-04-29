namespace Identity.Application.Outbox.Payloads;

public sealed record PasswordResetRequestedIntegrationEventPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long ResetTokenId,
    string RawResetToken,
    DateTime ExpiresAtUtc,
    string BusinessDedupeKey);