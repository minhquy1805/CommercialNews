using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;

public interface IProcessOutboxMessageUseCase
{
    Task<Result<ProcessOutboxMessageResponse>> ExecuteAsync(
        ProcessOutboxMessageRequest request,
        CancellationToken cancellationToken = default);
}