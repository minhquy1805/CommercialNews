using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.EmailDeliveries.ProcessPendingEmailDeliveries;

public static class ProcessPendingEmailDeliveriesValidator
{
    private const int MaxTopN = 200;

    public static Error? Validate(ProcessPendingEmailDeliveriesRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.TopN <= 0 || request.TopN > MaxTopN)
        {
            return NotificationsErrors.InvalidRequest;
        }

        return null;
    }
}