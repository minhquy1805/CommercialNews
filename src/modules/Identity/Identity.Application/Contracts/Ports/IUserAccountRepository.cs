using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Ports
{
    public interface IUserAccountRepository
    {
        Task<UserAccount?> GetByEmailNormalizedAsync(
            string emailNormalized,
            CancellationToken cancellationToken);

        Task<long> InsertAsync(
            UserAccount userAccount,
            CancellationToken cancellationToken);

        Task UpdateLastLoginAsync(
           long userId,
           CancellationToken cancellationToken);
    }
}
