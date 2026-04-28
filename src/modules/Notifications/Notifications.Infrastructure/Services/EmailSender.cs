using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;

namespace Notifications.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly NotificationEmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IOptions<NotificationEmailOptions> options,
        ILogger<EmailSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EmailSendResult> SendAsync(
        EmailSendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["MessageId"] = request.MessageId,
            ["TemplateKey"] = request.TemplateKey,
            ["CorrelationId"] = request.CorrelationId
        });

        try
        {
            MimeMessage message = BuildMessage(request);

            using SmtpClient client = new()
            {
                Timeout = _options.TimeoutMilliseconds
            };

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                GetSecureSocketOptions(_options),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                await client.AuthenticateAsync(
                    _options.UserName,
                    _options.Password,
                    cancellationToken);
            }

            string providerMessageId = await client.SendAsync(message, cancellationToken);

            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully. MessageId={MessageId}, TemplateKey={TemplateKey}, ProviderMessageId={ProviderMessageId}",
                request.MessageId,
                request.TemplateKey,
                providerMessageId);

            return new EmailSendResult
            {
                IsSuccess = true,
                IsAmbiguous = false,
                ProviderMessageId = providerMessageId
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException exception)
        {
            return Failure(
                isAmbiguous: true,
                providerErrorCode: "SMTP_TIMEOUT",
                providerErrorMessage: exception.Message);
        }
        catch (MailKit.CommandException exception)
        {
            return Failure(
                isAmbiguous: false,
                providerErrorCode: $"SMTP_COMMAND_{exception.Message}",
                providerErrorMessage: exception.Message);
        }
        catch (MailKit.ProtocolException exception)
        {
            return Failure(
                isAmbiguous: true,
                providerErrorCode: "SMTP_PROTOCOL",
                providerErrorMessage: exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected email send failure. MessageId={MessageId}, TemplateKey={TemplateKey}",
                request.MessageId,
                request.TemplateKey);

            return Failure(
                isAmbiguous: false,
                providerErrorCode: "SMTP_UNKNOWN",
                providerErrorMessage: exception.Message);
        }
    }

    private MimeMessage BuildMessage(EmailSendRequest request)
    {
        MimeMessage message = new();

        message.From.Add(new MailboxAddress(
            _options.FromName,
            _options.FromEmail));

        message.To.Add(MailboxAddress.Parse(request.ToEmail));

        message.Subject = request.Subject;

        message.Headers.Add("X-CommercialNews-MessageId", request.MessageId);
        message.Headers.Add("X-CommercialNews-TemplateKey", request.TemplateKey);

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            message.Headers.Add("X-Correlation-Id", request.CorrelationId);
        }

        message.Body = new BodyBuilder
        {
            HtmlBody = request.Body
        }.ToMessageBody();

        return message;
    }

    private EmailSendResult Failure(
        bool isAmbiguous,
        string providerErrorCode,
        string? providerErrorMessage)
    {
        _logger.LogWarning(
            "Email send failed. ProviderErrorCode={ProviderErrorCode}, IsAmbiguous={IsAmbiguous}",
            providerErrorCode,
            isAmbiguous);

        return new EmailSendResult
        {
            IsSuccess = false,
            IsAmbiguous = isAmbiguous,
            ProviderErrorCode = providerErrorCode,
            ProviderErrorMessage = SanitizeErrorMessage(providerErrorMessage)
        };
    }

    private static void ValidateRequest(EmailSendRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            throw new ArgumentException("Message id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.TemplateKey))
        {
            throw new ArgumentException("Template key is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("Email subject is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Email body is required.", nameof(request));
        }
    }

    private static SecureSocketOptions GetSecureSocketOptions(NotificationEmailOptions options)
    {
        return options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;
    }

    private static string SanitizeErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Email provider error.";
        }

        return message.Length <= 500
            ? message
            : message[..500];
    }
}