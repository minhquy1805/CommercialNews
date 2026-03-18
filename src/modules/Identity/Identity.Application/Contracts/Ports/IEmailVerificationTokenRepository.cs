using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Ports
{
    public interface IEmailVerificationTokenRepository
    {
        Task InsertAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken);
    }
}
