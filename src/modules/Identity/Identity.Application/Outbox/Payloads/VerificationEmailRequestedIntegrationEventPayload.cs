namespace Identity.Application.Outbox.Payloads;

public sealed record VerificationEmailRequestedIntegrationEventPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long VerificationTokenId,
    string RawVerificationToken,
    DateTime ExpiresAtUtc,
    string BusinessDedupeKey);