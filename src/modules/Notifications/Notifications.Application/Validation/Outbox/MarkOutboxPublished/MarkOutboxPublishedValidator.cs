using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.Outbox.MarkOutboxPublished;

public static class MarkOutboxPublishedValidator
{
    public static Error? Validate(MarkOutboxPublishedRequest? request)
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