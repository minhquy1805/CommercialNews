using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.Outbox.MarkOutboxDead;

public static class MarkOutboxDeadValidator
{
    private const int MaxLastErrorLength = 2000;
    private const int MaxLastErrorCodeLength = 100;
    private const int MaxLastErrorClassLength = 30;

    private static readonly HashSet<string> AllowedErrorClasses =
    [
        "Transient",
        "Permanent",
        "Ambiguous",
        "Policy",
        "Template",
        "Provider",
        "Validation"
    ];

    public static Error? Validate(MarkOutboxDeadRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.OutboxMessageId <= 0)
        {
            return NotificationsErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.LastError) &&
            request.LastError.Trim().Length > MaxLastErrorLength)
        {
            return NotificationsErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.LastErrorCode) &&
            request.LastErrorCode.Trim().Length > MaxLastErrorCodeLength)
        {
            return NotificationsErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.LastErrorClass))
        {
            string normalized = request.LastErrorClass.Trim();

            if (normalized.Length > MaxLastErrorClassLength)
            {
                return NotificationsErrors.InvalidRequest;
            }

            if (!AllowedErrorClasses.Contains(normalized))
            {
                return NotificationsErrors.InvalidRequest;
            }
        }

        return null;
    }
}