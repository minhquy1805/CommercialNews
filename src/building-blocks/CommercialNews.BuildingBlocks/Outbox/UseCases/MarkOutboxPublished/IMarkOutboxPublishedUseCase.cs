using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.MarkOutboxPublished;

public interface IMarkOutboxPublishedUseCase
{
    Task<Result<MarkOutboxPublishedResponse>> ExecuteAsync(
        MarkOutboxPublishedRequest request,
        CancellationToken cancellationToken = default);
}