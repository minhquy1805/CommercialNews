namespace Identity.Application.Contracts.Ports
{
    public interface IIdentityEmailSender
    {
        Task SendVerificationEmailAsync(
            string email,
            string publicId,
            string rawVerificationToken,
            CancellationToken cancellationToken);

        Task SendResetPasswordEmailAsync(
            string email,
            string publicId,
            string rawResetToken,
            CancellationToken cancellationToken);
    }
}
