namespace Notifications.Application.Consumers.Identity.Payloads;

public sealed record IdentityPasswordChangedPayload(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    string Reason,
    DateTime ChangedAtUtc,
    string BusinessDedupeKey);