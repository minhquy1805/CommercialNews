using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence
{
    public interface IEmailVerificationTokenRepository
    {
        Task InsertAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken = default);

        Task<EmailVerificationToken?> GetActiveByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken = default);

        Task<bool> MarkUsedAsync(
            long verificationTokenId,
            CancellationToken cancellationToken = default);
    }
}