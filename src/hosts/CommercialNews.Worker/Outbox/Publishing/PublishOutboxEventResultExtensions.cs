using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Outbox.Publishing;

public static class PublishOutboxEventResultExtensions
{
    public static Result<DispatchOutboxMessageResult> ToDispatchResult(
        this Result<PublishOutboxEventResult> publishResult)
    {
        ArgumentNullException.ThrowIfNull(publishResult);

        if (publishResult.IsFailure)
        {
            return Result<DispatchOutboxMessageResult>.Failure(
                publishResult.Error!);
        }

        PublishOutboxEventResult result = publishResult.Value!;

        if (result.Succeeded)
        {
            return Result<DispatchOutboxMessageResult>.Success(
                DispatchOutboxMessageResult.Success());
        }

        return Result<DispatchOutboxMessageResult>.Success(
            DispatchOutboxMessageResult.Failed(
                errorCode: result.ErrorCode,
                errorMessage: result.ErrorMessage,
                errorClass: result.ErrorClass,
                isRetryable: result.IsRetryable,
                isAmbiguous: result.IsAmbiguous));
    }
}