using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.ProcessEmailDelivery;

public interface IProcessEmailDeliveryUseCase
{
    Task<Result<ProcessEmailDeliveryResponse>> ExecuteAsync(
        ProcessEmailDeliveryRequest request,
        CancellationToken cancellationToken = default);
}