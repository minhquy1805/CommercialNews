using CommercialNews.Worker.Messaging.Outbox.Models;

namespace CommercialNews.Worker.Messaging.Authorization.Ports
{
    public interface IAuthorizationOutboxEventDispatcher
    {
        Task DispatchAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken);
    }
}