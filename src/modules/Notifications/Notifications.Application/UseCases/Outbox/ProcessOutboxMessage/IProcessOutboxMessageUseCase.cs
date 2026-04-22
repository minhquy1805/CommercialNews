using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;

/// <summary>
/// Phase note:
/// This use case currently lives in Notifications.Application because the only
/// active downstream processing path is notification delivery creation.
///
/// Future evolution:
/// If multiple modules begin consuming shared outbox messages through a common
/// runtime pipeline (for example Notifications, Audit, Search, Projections),
/// this orchestration should be promoted into a shared outbox dispatcher/runtime
/// in building-blocks, with module-specific handlers registered per event type.
/// </summary>
public interface IProcessOutboxMessageUseCase
{
    Task<Result<ProcessOutboxMessageResponse>> ExecuteAsync(
        ProcessOutboxMessageRequest request,
        CancellationToken cancellationToken = default);
}