using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.Outbox.ProcessOutboxMessage;

public static class ProcessOutboxMessageValidator
{
    public static Error? Validate(ProcessOutboxMessageRequest? request)
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