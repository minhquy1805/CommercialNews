using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface INotificationRetryPolicy
{
    RetryDecision Evaluate(
        NotificationRetryContext context);
}