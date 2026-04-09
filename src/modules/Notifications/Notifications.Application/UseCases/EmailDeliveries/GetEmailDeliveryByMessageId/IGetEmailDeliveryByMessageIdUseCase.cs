using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;

public interface IGetEmailDeliveryByMessageIdUseCase
{
    Task<Result<GetEmailDeliveryByIdResponse>> ExecuteAsync(
        GetEmailDeliveryByMessageIdRequest request,
        CancellationToken cancellationToken = default);
}