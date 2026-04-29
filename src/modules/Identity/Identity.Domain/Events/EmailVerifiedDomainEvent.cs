namespace Identity.Domain.Events;

public sealed record EmailVerifiedDomainEvent(
    long UserId,
    string UserPublicId,
    string Email,
    string? FullName,
    long VerificationTokenId,
    DateTime VerifiedAtUtc);