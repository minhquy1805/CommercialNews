using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Processing.Requests;
using Notifications.Application.Contracts.Processing.Responses;

namespace Notifications.Application.UseCases.Processing.ProcessEmailDelivery;

/// <summary>
/// TODO (deferred):
/// This runtime/internal processing use case is intentionally postponed.
/// It will be refactored after:
/// 1. Application service contract models are finalized
/// 2. Notifications ports are stabilized
/// 3. Core read/admin use cases are completed
/// Do not treat this implementation as final.
/// </summary>
public interface IProcessEmailDeliveryUseCase
{
    Task<Result<ProcessEmailDeliveryResponse>> ExecuteAsync(
        ProcessEmailDeliveryRequest request,
        CancellationToken cancellationToken = default);
}