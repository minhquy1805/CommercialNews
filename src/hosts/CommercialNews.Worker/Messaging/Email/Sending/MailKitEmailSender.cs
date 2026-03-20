using CommercialNews.Worker.Messaging.Email.Configuration;
using CommercialNews.Worker.Messaging.Email.Ports;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CommercialNews.Worker.Messaging.Email.Sending
{
    public sealed class MailKitEmailSender : IWorkerEmailSender
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(
            IOptions<MailSettings> mailSettings,
            ILogger<MailKitEmailSender> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }

        public async Task<string?> SendAsync(
            string toEmail,
            string subject,
            string body,
            CancellationToken cancellationToken)
        {
            ValidateSettings();

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_mailSettings.FromName, _mailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var smtpClient = new SmtpClient();

            try
            {
                var socketOptions = _mailSettings.UseSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTlsWhenAvailable;

                await smtpClient.ConnectAsync(
                    _mailSettings.Host,
                    _mailSettings.Port,
                    socketOptions,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(_mailSettings.UserName))
                {
                    await smtpClient.AuthenticateAsync(
                        _mailSettings.UserName,
                        _mailSettings.Password,
                        cancellationToken);
                }

                var response = await smtpClient.SendAsync(message, cancellationToken);

                await smtpClient.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation(
                    "Email sent via MailKit. To={ToEmail}, Subject={Subject}, Response={Response}",
                    toEmail,
                    subject,
                    response);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "MailKit failed to send email. To={ToEmail}, Subject={Subject}",
                    toEmail,
                    subject);

                throw;
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_mailSettings.Host))
            {
                throw new InvalidOperationException("MailSettings:Host is required.");
            }

            if (_mailSettings.Port <= 0)
            {
                throw new InvalidOperationException("MailSettings:Port must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(_mailSettings.FromEmail))
            {
                throw new InvalidOperationException("MailSettings:FromEmail is required.");
            }

            if (string.IsNullOrWhiteSpace(_mailSettings.FromName))
            {
                throw new InvalidOperationException("MailSettings:FromName is required.");
            }
        }
    }
}

