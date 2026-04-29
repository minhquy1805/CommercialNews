namespace Identity.Domain.Events;

public sealed record EmailVerificationTokenIssuedDomainEvent(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long VerificationTokenId,
    DateTime ExpiresAtUtc,
    DateTime OccurredAtUtc);