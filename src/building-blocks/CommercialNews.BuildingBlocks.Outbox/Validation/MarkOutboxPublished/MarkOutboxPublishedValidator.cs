using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Validation.MarkOutboxPublished;

public static class MarkOutboxPublishedValidator
{
    public static Error? Validate(MarkOutboxPublishedRequest? request)
    {
        if (request is null)
        {
            return OutboxErrors.ValidationFailed;
        }

        if (request.OutboxMessageId <= 0)
        {
            return OutboxErrors.InvalidRequest;
        }

        return null;
    }
}