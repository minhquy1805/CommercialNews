namespace CommercialNews.BuildingBlocks.Messaging.Outbox
{
    public interface IOutboxWriter
    {
        Task WriteAsync(
            string messageId,
            string eventType,
            string aggregateType,
            string aggregateId,
            string? aggregatePublicId,
            int? aggregateVersion,
            string payload,
            string? headers,
            string? correlationId,
            long? initiatorUserId,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken);
    }
}