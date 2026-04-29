namespace Identity.Domain.Events;

public sealed record PasswordChangedDomainEvent(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    string Reason,
    DateTime ChangedAtUtc);