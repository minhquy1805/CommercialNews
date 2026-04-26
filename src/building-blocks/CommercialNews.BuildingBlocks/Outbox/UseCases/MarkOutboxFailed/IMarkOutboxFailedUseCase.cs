using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxFailed;

public interface IMarkOutboxFailedUseCase
{
    Task<Result<MarkOutboxFailedResponse>> ExecuteAsync(
        MarkOutboxFailedRequest request,
        CancellationToken cancellationToken = default);
}