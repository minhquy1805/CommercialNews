using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Ports
{
    public interface IPasswordResetTokenRepository
    {
        Task RevokeActiveByUserIdAsync(
            long userId,
            CancellationToken cancellationToken);

        Task InsertAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken);
    }
}
