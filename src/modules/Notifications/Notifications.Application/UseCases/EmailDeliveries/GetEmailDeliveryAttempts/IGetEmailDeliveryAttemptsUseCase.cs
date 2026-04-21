using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;

public interface IGetEmailDeliveryAttemptsUseCase
{
    Task<Result<GetEmailDeliveryAttemptsResponse>> ExecuteAsync(
        GetEmailDeliveryAttemptsRequest request,
        CancellationToken cancellationToken = default);
}