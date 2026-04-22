using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.ProcessPendingOutboxMessages;

/// <summary>
/// Phase note:
/// This batch use case currently lives in Notifications.Application because the
/// active downstream processing path is notification delivery creation.
///
/// Future evolution:
/// If multiple modules begin consuming shared outbox messages through a common
/// runtime pipeline, this orchestration should be promoted into a shared outbox
/// dispatcher/runtime in building-blocks, with module-specific handlers registered
/// per event type.
/// </summary>
public interface IProcessPendingOutboxMessagesUseCase
{
    Task<Result<ProcessPendingOutboxMessagesResponse>> ExecuteAsync(
        ProcessPendingOutboxMessagesRequest request,
        CancellationToken cancellationToken = default);
}