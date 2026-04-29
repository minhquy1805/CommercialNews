using Identity.Application.Ports.Persistence;

namespace Identity.Application.Ports.Services;

public interface IIdentityOutboxWriter
{
    Task<long> EnqueueVerificationEmailRequestedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        long verificationTokenId,
        string rawVerificationToken,
        DateTime expiresAtUtc,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueuePasswordResetRequestedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        long resetTokenId,
        string rawResetToken,
        DateTime expiresAtUtc,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueuePasswordChangedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string reason,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueEmailVerifiedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        long verificationTokenId,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken = default);
}