using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.Outbox.GetOutboxMessageByMessageId;

public static class GetOutboxMessageByMessageIdValidator
{
    private const int MessageIdLength = 26;

    public static Error? Validate(GetOutboxMessageByMessageIdRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            return NotificationsErrors.InvalidMessageId;
        }

        if (request.MessageId.Trim().Length != MessageIdLength)
        {
            return NotificationsErrors.InvalidMessageId;
        }

        return null;
    }
}