using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Errors;

namespace Notifications.Application.Validation.Outbox.ProcessPendingOutboxMessages;

public static class ProcessPendingOutboxMessagesValidator
{
    private const int MaxBatchSize = 200;

    public static Error? Validate(ProcessPendingOutboxMessagesRequest? request)
    {
        if (request is null)
        {
            return NotificationsErrors.ValidationFailed;
        }

        if (request.BatchSize <= 0 || request.BatchSize > MaxBatchSize)
        {
            return NotificationsErrors.InvalidRequest;
        }

        return null;
    }
}