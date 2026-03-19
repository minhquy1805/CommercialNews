namespace CommercialNews.Worker.Messaging.Email.Ports
{
    public interface IEmailDeliveryRepository
    {
        Task<long> InsertAsync(
            string messageId,
            long? userId,
            string toEmail,
            string templateKey,
            string? subject,
            string? correlationId,
            CancellationToken cancellationToken);

        Task MarkSentAsync(
            long emailDeliveryId,
            string? providerMessageId,
            CancellationToken cancellationToken);

        Task MarkFailedAsync(
            long emailDeliveryId,
            DateTime? nextRetryAt,
            string? lastError,
            CancellationToken cancellationToken);

        Task MarkDeadLetterAsync(
            long emailDeliveryId,
            string? lastError,
            CancellationToken cancellationToken);
    }
}