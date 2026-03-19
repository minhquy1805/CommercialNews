namespace CommercialNews.Worker.Messaging.Outbox.Ports
{
    
    public interface IOutboxMessageStateRepository
    {
        Task MarkProcessingAsync(long outboxMessageId, CancellationToken cancellationToken);
        Task MarkPublishedAsync(long outboxMessageId, CancellationToken cancellationToken);
        Task MarkFailedAsync(long outboxMessageId, DateTime? nextRetryAt, string? lastError, CancellationToken cancellationToken);
        Task MarkDeadLetterAsync(long outboxMessageId, string? lastError, CancellationToken cancellationToken);
    }
}