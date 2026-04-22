using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxDead;

/// <summary>
/// Marks a single outbox message as dead when it should no longer be retried.
/// </summary>
public interface IMarkOutboxDeadUseCase
{
    Task<Result<MarkOutboxDeadResponse>> ExecuteAsync(
        MarkOutboxDeadRequest request,
        CancellationToken cancellationToken = default);
}