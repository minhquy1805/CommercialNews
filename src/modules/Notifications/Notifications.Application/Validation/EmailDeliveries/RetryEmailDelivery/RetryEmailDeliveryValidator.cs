using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.EmailDeliveries.RetryEmailDelivery;

public static class RetryEmailDeliveryValidator
{
    public static Error? Validate(RetryEmailDeliveryRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.EmailDeliveryId <= 0)
        {
            return NotificationsErrors.InvalidRequest;
        }

        if (request.ActorUserId is not null && request.ActorUserId <= 0)
        {
            return NotificationsErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId) &&
            request.CorrelationId.Trim().Length > 100)
        {
            return NotificationsErrors.InvalidRequest;
        }

        return null;
    }
}