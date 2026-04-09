using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Services;

namespace Notifications.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly NotificationEmailOptions _options;

    public EmailSender(IOptions<NotificationEmailOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<EmailSendResult> SendAsync(
        EmailSendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderRejected,
                ProviderErrorMessage = "Recipient email is required."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderRejected,
                ProviderErrorMessage = "Email body is required."
            };
        }

        MimeMessage message = BuildMimeMessage(request);

        try
        {
            using SmtpClient client = new();

            // Important:
            // Timeout is set explicitly so that transport issues surface as
            // structured failures instead of hanging indefinitely.
            client.Timeout = _options.TimeoutMilliseconds;

            SecureSocketOptions socketOptions = ResolveSocketOptions();

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                socketOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                await client.AuthenticateAsync(
                    _options.UserName,
                    _options.Password,
                    cancellationToken);
            }

            string? providerMessageId = await client.SendAsync(message, cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult
            {
                IsSuccess = true,
                IsAmbiguous = false,
                ProviderMessageId = providerMessageId,
                ProviderErrorCode = null,
                ProviderErrorMessage = null
            };
        }
        catch (TimeoutException exception)
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.NetworkTimeout,
                ProviderErrorMessage = exception.Message
            };
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Important:
            // A timeout-like cancellation without an external caller cancellation
            // may mean we lost certainty during send. We treat it as ambiguous.
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = true,
                ProviderErrorCode = NotificationServiceErrorCodes.AmbiguousTimeout,
                ProviderErrorMessage = exception.Message
            };
        }
        catch (SocketException exception)
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.NetworkTimeout,
                ProviderErrorMessage = exception.Message
            };
        }
        catch (ServiceNotConnectedException exception)
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderTemporaryUnavailable,
                ProviderErrorMessage = exception.Message
            };
        }
        catch (ServiceNotAuthenticatedException exception)
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderRejected,
                ProviderErrorMessage = exception.Message
            };
        }
        catch (SmtpCommandException exception)
        {
            return MapSmtpCommandException(exception);
        }
        catch (SmtpProtocolException exception)
        {
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderTemporaryUnavailable,
                ProviderErrorMessage = exception.Message
            };
        }
        catch (Exception exception)
        {
            // Important:
            // Unknown transport/provider failures are normalized here so the
            // classifier and retry policy receive a stable shape.
            return new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderRejected,
                ProviderErrorMessage = exception.Message
            };
        }
    }

    private MimeMessage BuildMimeMessage(EmailSendRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("NotificationEmailOptions.FromEmail is required.");
        }

        MimeMessage message = new();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(request.ToEmail));
        message.Subject = request.Subject ?? string.Empty;

        // Important:
        // Phase 1 keeps the renderer output simple and sends it as plain text.
        // You can switch this to HTML later if your renderer starts producing HTML.
        message.Body = new TextPart("plain")
        {
            Text = request.Body
        };

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            message.Headers.Add("X-Correlation-Id", request.CorrelationId);
        }

        return message;
    }

    private SecureSocketOptions ResolveSocketOptions()
    {
        if (_options.UseSsl)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        // Important:
        // StartTlsWhenAvailable is a practical phase-1 default when SSL-on-connect
        // is not explicitly required.
        return SecureSocketOptions.StartTlsWhenAvailable;
    }

    private static EmailSendResult MapSmtpCommandException(SmtpCommandException exception)
    {
        return exception.StatusCode switch
        {
            SmtpStatusCode.MailboxUnavailable => new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.Smtp550,
                ProviderErrorMessage = exception.Message
            },

            SmtpStatusCode.TransactionFailed => new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.Smtp451,
                ProviderErrorMessage = exception.Message
            },

            SmtpStatusCode.ServiceNotAvailable => new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.Smtp421,
                ProviderErrorMessage = exception.Message
            },

            _ => new EmailSendResult
            {
                IsSuccess = false,
                IsAmbiguous = false,
                ProviderErrorCode = NotificationServiceErrorCodes.ProviderRejected,
                ProviderErrorMessage = exception.Message
            }
        };
    }
}