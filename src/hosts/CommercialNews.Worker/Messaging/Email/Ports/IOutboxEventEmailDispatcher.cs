using CommercialNews.Worker.Messaging.Outbox.Models;

namespace CommercialNews.Worker.Messaging.Email.Ports
{
    public interface IOutboxEventEmailDispatcher
    {
        Task DispatchAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken);
    }
}


