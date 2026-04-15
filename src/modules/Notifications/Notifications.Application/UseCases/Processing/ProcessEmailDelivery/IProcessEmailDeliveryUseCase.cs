using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Processing.Requests;
using Notifications.Application.Contracts.Processing.Responses;

namespace Notifications.Application.UseCases.Processing.ProcessEmailDelivery;

public interface IProcessEmailDeliveryUseCase
{
    Task<Result<ProcessEmailDeliveryResponse>> ExecuteAsync(
        ProcessEmailDeliveryRequest request,
        CancellationToken cancellationToken = default);
}