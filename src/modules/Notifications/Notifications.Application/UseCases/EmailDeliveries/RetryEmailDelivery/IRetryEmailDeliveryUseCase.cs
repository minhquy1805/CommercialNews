using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;

public interface IRetryEmailDeliveryUseCase
{
    Task<Result<RetryEmailDeliveryResponse>> ExecuteAsync(
        RetryEmailDeliveryRequest request,
        CancellationToken cancellationToken = default);
}