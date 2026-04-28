namespace Notifications.Application.Outbox;

public static class NotificationsIntegrationEventTypes
{
    public const string EmailSent = "notifications.email.sent";

    public const string EmailFailed = "notifications.email.failed";

    public const string EmailDead = "notifications.email.dead";
}