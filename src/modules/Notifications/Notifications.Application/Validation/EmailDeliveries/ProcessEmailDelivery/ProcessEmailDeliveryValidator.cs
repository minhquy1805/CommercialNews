using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.EmailDeliveries.ProcessEmailDelivery;

public static class ProcessEmailDeliveryValidator
{
    public static Error? Validate(ProcessEmailDeliveryRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.EmailDeliveryId <= 0)
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