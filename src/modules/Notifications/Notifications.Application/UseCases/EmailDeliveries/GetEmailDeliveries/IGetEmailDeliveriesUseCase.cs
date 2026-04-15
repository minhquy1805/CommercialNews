using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;

public interface IGetEmailDeliveriesUseCase
{
    Task<Result<GetEmailDeliveriesResponse>> ExecuteAsync(
        GetEmailDeliveriesRequest request,
        CancellationToken cancellationToken = default);
}