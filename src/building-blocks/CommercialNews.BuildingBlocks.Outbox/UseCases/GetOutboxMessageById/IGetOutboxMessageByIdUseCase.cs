using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.GetOutboxMessageById;

public interface IGetOutboxMessageByIdUseCase
{
    Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByIdRequest request,
        CancellationToken cancellationToken = default);
}