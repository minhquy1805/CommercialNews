using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface INotificationDedupeService
{
    Task<DedupeCheckResult> CheckAsync(
        NotificationDedupeCheckRequest request,
        CancellationToken cancellationToken = default);
}