using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence;

public interface IPasswordResetTokenRepository
{
    Task InsertAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetActiveByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    Task<int> RevokeActiveByUserIdAsync(
        long userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> MarkUsedAsync(
        long resetTokenId,
        DateTime usedAtUtc,
        CancellationToken cancellationToken = default);
}