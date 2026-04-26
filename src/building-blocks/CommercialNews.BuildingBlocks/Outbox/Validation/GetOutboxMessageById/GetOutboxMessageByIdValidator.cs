using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Validation.GetOutboxMessageById;

public static class GetOutboxMessageByIdValidator
{
    public static Error? Validate(GetOutboxMessageByIdRequest? request)
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