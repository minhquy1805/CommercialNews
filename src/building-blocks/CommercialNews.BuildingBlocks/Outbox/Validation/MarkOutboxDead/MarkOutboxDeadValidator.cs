using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Validation.MarkOutboxDead;

public static class MarkOutboxDeadValidator
{
    private const int MaxLastErrorLength = 2000;
    private const int MaxLastErrorCodeLength = 100;
    private const int MaxLastErrorClassLength = 30;

    public static Error? Validate(MarkOutboxDeadRequest? request)
    {
        if (request is null)
        {
            return OutboxErrors.ValidationFailed;
        }

        if (request.OutboxMessageId <= 0)
        {
            return OutboxErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.LastError) &&
            request.LastError.Trim().Length > MaxLastErrorLength)
        {
            return OutboxErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.LastErrorCode) &&
            request.LastErrorCode.Trim().Length > MaxLastErrorCodeLength)
        {
            return OutboxErrors.InvalidRequest;
        }

        if (!string.IsNullOrWhiteSpace(request.LastErrorClass))
        {
            string normalized = request.LastErrorClass.Trim();

            if (normalized.Length > MaxLastErrorClassLength)
            {
                return OutboxErrors.InvalidRequest;
            }

            if (!OutboxFailureClass.IsValid(normalized))
            {
                return OutboxErrors.InvalidRequest;
            }
        }

        return null;
    }
}