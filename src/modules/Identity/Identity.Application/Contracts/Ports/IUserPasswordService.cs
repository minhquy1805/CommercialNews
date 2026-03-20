namespace Identity.Application.Contracts.Ports
{
    public interface IUserPasswordService
    {
        Task UpdatePasswordAsync(
            long userId,
            string newPasswordHash,
            CancellationToken cancellationToken);
    }
}
