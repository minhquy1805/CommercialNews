using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveryByMessageId;

public static class GetEmailDeliveryByMessageIdValidator
{
    private const int MessageIdLength = 26;

    public static Error? Validate(GetEmailDeliveryByMessageIdRequest? request)
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