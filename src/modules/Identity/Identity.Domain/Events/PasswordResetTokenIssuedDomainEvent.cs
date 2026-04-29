namespace Identity.Domain.Events;

public sealed record PasswordResetTokenIssuedDomainEvent(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long ResetTokenId,
    DateTime ExpiresAtUtc,
    DateTime OccurredAtUtc);