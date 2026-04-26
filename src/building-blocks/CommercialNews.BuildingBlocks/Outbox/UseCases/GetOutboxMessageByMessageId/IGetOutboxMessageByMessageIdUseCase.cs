using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.GetOutboxMessageByMessageId;

public interface IGetOutboxMessageByMessageIdUseCase
{
    Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByMessageIdRequest request,
        CancellationToken cancellationToken = default);
}