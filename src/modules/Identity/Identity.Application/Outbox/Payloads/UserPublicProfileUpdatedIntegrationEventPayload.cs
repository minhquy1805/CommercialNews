namespace Identity.Application.Outbox.Payloads;

public sealed record UserPublicProfileUpdatedIntegrationEventPayload(
    long UserId,
    string UserPublicId,
    string? FullName,
    string? AvatarUrl,
    int Version,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);
