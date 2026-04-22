using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.Outbox.GetOutboxMessageById;

public static class GetOutboxMessageByIdValidator
{
    public static Error? Validate(GetOutboxMessageByIdRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.OutboxMessageId <= 0)
        {
            return NotificationsErrors.InvalidRequest;
        }

        return null;
    }
}