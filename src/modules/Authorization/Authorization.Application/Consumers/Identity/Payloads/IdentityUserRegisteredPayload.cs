namespace Authorization.Application.Consumers.Identity.Payloads;

public sealed record IdentityUserRegisteredPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    string Status,
    DateTime RegisteredAtUtc,
    string BusinessDedupeKey);
