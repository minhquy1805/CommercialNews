using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveryById;

public static class GetEmailDeliveryByIdValidator
{
    public static Error? Validate(GetEmailDeliveryByIdRequest? request)
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