using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;

public interface IGetEmailDeliveryByIdUseCase
{
    Task<Result<GetEmailDeliveryByIdResponse>> ExecuteAsync(
        GetEmailDeliveryByIdRequest request,
        CancellationToken cancellationToken = default);
}