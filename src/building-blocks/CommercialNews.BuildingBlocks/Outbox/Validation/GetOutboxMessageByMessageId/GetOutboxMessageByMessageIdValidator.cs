using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Validation.GetOutboxMessageByMessageId;

public static class GetOutboxMessageByMessageIdValidator
{
    private const int MessageIdLength = 26;

    public static Error? Validate(GetOutboxMessageByMessageIdRequest? request)
    {
        if (request is null)
        {
            return OutboxErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            return OutboxErrors.InvalidMessageId;
        }

        if (request.MessageId.Trim().Length != MessageIdLength)
        {
            return OutboxErrors.InvalidMessageId;
        }

        return null;
    }
}