using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public interface IOutboxBatchProcessor
{
    Task<Result<ProcessPendingOutboxMessagesResponse>> ProcessAsync(
        ProcessPendingOutboxMessagesRequest request,
        CancellationToken cancellationToken = default);
}