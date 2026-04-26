using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public interface IOutboxDispatcher
{
    Task<Result<DispatchOutboxMessageResult>> DispatchAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);
}