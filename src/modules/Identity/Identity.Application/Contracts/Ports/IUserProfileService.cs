namespace Identity.Application.Contracts.Ports
{
    public interface IUserProfileService
    {
        Task UpdateProfileAsync(
            long userId,
            string? fullName,
            string? avatarUrl,
            CancellationToken cancellationToken);
    }
}