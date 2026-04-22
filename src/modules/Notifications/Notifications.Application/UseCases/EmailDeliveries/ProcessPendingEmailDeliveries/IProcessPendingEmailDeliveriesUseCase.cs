using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;

namespace Notifications.Application.UseCases.EmailDeliveries.ProcessPendingEmailDeliveries;

public interface IProcessPendingEmailDeliveriesUseCase
{
    Task<Result<ProcessPendingEmailDeliveriesResponse>> ExecuteAsync(
        ProcessPendingEmailDeliveriesRequest request,
        CancellationToken cancellationToken = default);
}