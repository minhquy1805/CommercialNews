using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxPublished;

/// <summary>
/// TODO (deferred):
/// This outbox use case is intentionally postponed.
/// Outbox is being moved toward a shared/building-blocks ownership model.
/// Do not treat this interface/implementation as finalized inside Notifications.
/// </summary>
public interface IMarkOutboxPublishedUseCase
{
    Task<Result<MarkOutboxPublishedResponse>> ExecuteAsync(
        MarkOutboxPublishedRequest request,
        CancellationToken cancellationToken = default);
}