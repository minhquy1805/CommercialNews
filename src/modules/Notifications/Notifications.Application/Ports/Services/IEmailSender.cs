using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        EmailSendRequest request,
        CancellationToken cancellationToken = default);
}