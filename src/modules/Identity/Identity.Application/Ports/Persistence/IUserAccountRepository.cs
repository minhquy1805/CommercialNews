using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByEmailNormalizedAsync(
        string emailNormalized,
        CancellationToken cancellationToken = default);

    Task<long> InsertAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateProfileAsync(
        long userId,
        string? fullName,
        string? avatarUrl,
        CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordAsync(
        long userId,
        string passwordHash,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateLastLoginAsync(
        long userId,
        DateTime lastLoginAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> MarkEmailVerifiedAsync(
        long userId,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        long userId,
        string status,
        DateTime? lockedUntil,
        CancellationToken cancellationToken = default);

    Task<long> InsertBootstrapAdminAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken = default);
}