using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence
{
    public interface IPasswordResetTokenRepository
    {
        Task RevokeActiveByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default);

        Task InsertAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default);

        Task<PasswordResetToken?> GetActiveByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken = default);

        Task<bool> MarkUsedAsync(
            long resetTokenId,
            CancellationToken cancellationToken = default);
    }
}