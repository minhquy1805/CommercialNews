using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface IEmailDeliveryDedupeService
{
    Task<EmailDeliveryDedupeCheckResult> CheckAsync(
        EmailDeliveryDedupeCheckRequest request,
        CancellationToken cancellationToken = default);
}