using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Errors;
using Notifications.Domain.Enums;

namespace Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveries;

public static class GetEmailDeliveriesValidator
{
    private const int MaxPageSize = 100;
    private const int MessageIdLength = 26;

    public static Error? Validate(GetEmailDeliveriesRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.Page <= 0 || request.PageSize <= 0 || request.PageSize > MaxPageSize)
        {
            return NotificationsErrors.InvalidQuery;
        }

        if (request.FromCreatedAt is not null &&
            request.ToCreatedAt is not null &&
            request.FromCreatedAt >= request.ToCreatedAt)
        {
            return NotificationsErrors.InvalidQuery;
        }

        if (request.RecipientUserId is not null && request.RecipientUserId <= 0)
        {
            return NotificationsErrors.InvalidQuery;
        }

        if (!string.IsNullOrWhiteSpace(request.TemplateKey) &&
            !NotificationTemplateKey.IsValid(request.TemplateKey))
        {
            return NotificationsErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !EmailDeliveryStatus.IsValid(request.Status))
        {
            return NotificationsErrors.InvalidQuery;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId) &&
            request.CorrelationId.Trim().Length > 100)
        {
            return NotificationsErrors.InvalidQuery;
        }

        if (!string.IsNullOrWhiteSpace(request.MessageId) &&
            request.MessageId.Trim().Length != MessageIdLength)
        {
            return NotificationsErrors.InvalidMessageId;
        }

        return null;
    }
}