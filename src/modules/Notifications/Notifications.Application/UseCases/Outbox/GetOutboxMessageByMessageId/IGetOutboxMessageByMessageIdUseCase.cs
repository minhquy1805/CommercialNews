using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.GetOutboxMessageByMessageId;

public interface IGetOutboxMessageByMessageIdUseCase
{
    Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByMessageIdRequest request,
        CancellationToken cancellationToken = default);
}