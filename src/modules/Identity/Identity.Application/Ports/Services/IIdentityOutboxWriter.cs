using Identity.Application.Ports.Persistence;

namespace Identity.Application.Ports.Services;

public interface IIdentityOutboxWriter
{
    Task<long> EnqueueUserRegisteredAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string status,
        int version,
        DateTime registeredAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueUserPublicProfileUpdatedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string? fullName,
        string? avatarUrl,
        int version,
        DateTime updatedAtUtc,
        string? correlationId,
        CancellationToken cancellationToken = default);

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

    Task<long> EnqueueUserActivatedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        DateTime activatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueUserDisabledAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        bool sessionsRevoked,
        int revokedSessionCount,
        DateTime disabledAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueUserLockedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        DateTime lockedUntilUtc,
        bool sessionsRevoked,
        int revokedSessionCount,
        DateTime lockedAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueUserUnlockedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        DateTime unlockedAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueEmailMarkedVerifiedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        bool wasAlreadyVerified,
        string previousStatus,
        string newStatus,
        DateTime markedVerifiedAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueUserSessionsRevokedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        int revokedSessionCount,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);
}
