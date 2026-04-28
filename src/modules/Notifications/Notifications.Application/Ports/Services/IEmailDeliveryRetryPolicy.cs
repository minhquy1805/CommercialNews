using Notifications.Application.Contracts.Services;

namespace Notifications.Application.Ports.Services;

public interface IEmailDeliveryRetryPolicy
{
    RetryDecision Evaluate(
        EmailDeliveryRetryContext context);
}