namespace Identity.Application.Outbox.Payloads;

public sealed record PasswordChangedIntegrationEventPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    string Reason,
    DateTime ChangedAtUtc,
    string BusinessDedupeKey);