namespace Notifications.Application.Contracts.Ingestion;

public sealed class NotificationIngestionResult
{
    public long EmailDeliveryId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string BusinessDedupeKey { get; init; } = string.Empty;

    public bool WasInserted { get; init; }

    public bool WasDeduped => !WasInserted;

    public static NotificationIngestionResult Inserted(
        long emailDeliveryId,
        string messageId,
        string businessDedupeKey)
    {
        return new NotificationIngestionResult
        {
            EmailDeliveryId = emailDeliveryId,
            MessageId = messageId,
            BusinessDedupeKey = businessDedupeKey,
            WasInserted = true
        };
    }

    public static NotificationIngestionResult Deduped(
        long emailDeliveryId,
        string messageId,
        string businessDedupeKey)
    {
        return new NotificationIngestionResult
        {
            EmailDeliveryId = emailDeliveryId,
            MessageId = messageId,
            BusinessDedupeKey = businessDedupeKey,
            WasInserted = false
        };
    }
}