namespace Identity.Application.Outbox.Payloads;

public sealed record EmailVerifiedIntegrationEventPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long VerificationTokenId,
    DateTime VerifiedAtUtc,
    string BusinessDedupeKey);