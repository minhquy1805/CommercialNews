using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveryAttempts;

public static class GetEmailDeliveryAttemptsValidator
{
    public static Error? Validate(GetEmailDeliveryAttemptsRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.EmailDeliveryId <= 0)
        {
            return NotificationsErrors.InvalidRequest;
        }

        return null;
    }
}