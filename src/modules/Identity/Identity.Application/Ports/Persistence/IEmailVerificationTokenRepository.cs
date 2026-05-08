using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence;

public interface IEmailVerificationTokenRepository
{
    Task<long> InsertAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> GetActiveByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailVerificationToken>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkUsedAsync(
        long verificationTokenId,
        DateTime usedAtUtc,
        CancellationToken cancellationToken = default);
}