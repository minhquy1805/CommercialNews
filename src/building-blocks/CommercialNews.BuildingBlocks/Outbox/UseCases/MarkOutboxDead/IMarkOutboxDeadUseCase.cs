using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxDead;

public interface IMarkOutboxDeadUseCase
{
    Task<Result<MarkOutboxDeadResponse>> ExecuteAsync(
        MarkOutboxDeadRequest request,
        CancellationToken cancellationToken = default);
}