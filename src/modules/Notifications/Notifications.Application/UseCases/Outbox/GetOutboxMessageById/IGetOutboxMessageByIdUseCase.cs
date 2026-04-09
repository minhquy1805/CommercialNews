using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;

namespace Notifications.Application.UseCases.Outbox.GetOutboxMessageById;

public interface IGetOutboxMessageByIdUseCase
{
    Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByIdRequest request,
        CancellationToken cancellationToken = default);
}