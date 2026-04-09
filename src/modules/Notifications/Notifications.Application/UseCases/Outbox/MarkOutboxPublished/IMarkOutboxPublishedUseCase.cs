using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxPublished;

public interface IMarkOutboxPublishedUseCase
{
    Task<Result<MarkOutboxPublishedResponse>> ExecuteAsync(
        MarkOutboxPublishedRequest request,
        CancellationToken cancellationToken = default);
}