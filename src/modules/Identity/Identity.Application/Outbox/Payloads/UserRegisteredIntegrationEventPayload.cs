namespace Identity.Application.Outbox.Payloads;

public sealed record UserRegisteredIntegrationEventPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    string Status,
    DateTime RegisteredAtUtc,
    string BusinessDedupeKey);
