using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

/// <summary>
/// TODO (deferred):
/// This outbox use case is intentionally postponed.
/// Outbox is being moved toward a shared/building-blocks ownership model.
/// Do not treat this interface/implementation as finalized inside Notifications.
/// </summary>
namespace Notifications.Application.UseCases.Outbox.MarkOutboxDeadLetter;

public interface IMarkOutboxDeadLetterUseCase
{
    Task<Result<MarkOutboxDeadLetterResponse>> ExecuteAsync(
        MarkOutboxDeadLetterRequest request,
        CancellationToken cancellationToken = default);
}