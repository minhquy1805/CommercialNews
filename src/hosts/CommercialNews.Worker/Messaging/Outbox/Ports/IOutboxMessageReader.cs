using CommercialNews.Worker.Messaging.Outbox.Models;

namespace CommercialNews.Worker.Messaging.Outbox.Ports
{
    
    public interface IOutboxMessageReader
    {
        Task<IReadOnlyList<OutboxMessageRecord>> SelectPendingAsync(
            int topN,
            DateTime? nowUtc,
            CancellationToken cancellationToken);
    }
}