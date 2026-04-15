using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxFailed;

public interface IMarkOutboxFailedUseCase
{
    Task<Result<MarkOutboxFailedResponse>> ExecuteAsync(
        MarkOutboxFailedRequest request,
        CancellationToken cancellationToken = default);
}