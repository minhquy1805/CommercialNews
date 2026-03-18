using Identity.Application.Contracts.Ports;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Messaging
{
    public sealed class IdentityEmailSender : IIdentityEmailSender
    {
        private readonly ILogger<IdentityEmailSender> _logger;

        public IdentityEmailSender(ILogger<IdentityEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendVerificationEmailAsync(
            string email,
            string publicId,
            string rawVerificationToken,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Verification email requested. Email={Email}, PublicId={PublicId}, RawToken={RawToken}",
                email,
                publicId,
                rawVerificationToken);

            return Task.CompletedTask;
        }

        public Task SendResetPasswordEmailAsync(
            string email,
            string publicId,
            string rawResetToken,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Reset password email requested. Email={Email}, PublicId={PublicId}, RawToken={RawToken}",
                email,
                publicId,
                rawResetToken);

            return Task.CompletedTask;
        }
    }
}
