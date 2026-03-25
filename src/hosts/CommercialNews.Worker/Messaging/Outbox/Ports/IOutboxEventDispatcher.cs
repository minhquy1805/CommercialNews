using CommercialNews.Worker.Messaging.Outbox.Models;

namespace CommercialNews.Worker.Messaging.Outbox.Ports
{
    public interface IOutboxEventDispatcher
    {
        Task DispatchAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken);
    }
}