using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Worker.Outbox.Publishing;

public interface IOutboxEventPublisher
{
    Task<Result<PublishOutboxEventResult>> PublishAsync(
        OutboxMessage message,
        string routingKey,
        CancellationToken cancellationToken = default);
}