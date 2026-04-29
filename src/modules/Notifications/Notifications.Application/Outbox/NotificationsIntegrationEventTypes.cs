namespace Notifications.Application.Outbox;

public static class NotificationsIntegrationEventTypes
{
    public const string EmailSent = "notifications.email_sent";

    public const string EmailFailed = "notifications.email_failed";

    public const string EmailDead = "notifications.email_dead";
}